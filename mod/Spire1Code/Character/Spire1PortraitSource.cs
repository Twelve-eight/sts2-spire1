using System;
using System.Collections.Generic;
using System.Text.Json;
using Godot;
using MegaCrit.Sts2.Core.Models;

namespace Spire1.Spire1Code.Character;

/// <summary>
/// Presentation-only bindings produced from the independently reviewed legacy atlas manifest.
/// This does not register cards, alter a model, or decide which cards belong to a pool.
/// The manifest and textures are packaged separately from the code.
/// </summary>
internal static class Spire1PortraitSource
{
    private const string ManifestPath = "res://Spire1/legacy_portraits.json";
    private const string TextureRoot = "res://Spire1/images/card_portraits/";
    private const ulong MaximumManifestBytes = 256 * 1024;
    private const int MaximumRows = 1024;

    // Identity is the exact canonical ModelId pair. Runtime card models may be mutable clones, so the
    // runtime type name is not an identity; the manifest still validates each exact type once on load.
    private readonly record struct Key(ModelId CharacterId, ModelId CardId);

    // These are this mod's own additive StS1 characters (Spire1.Spire1Code.Character.Ironclad /
    // Silent / Defect, which resolve in this namespace), never the native StS2 characters that share
    // the same short name. A native ironclad/silent/defect model has a different ModelId and stays native.
    private static readonly ModelId IroncladId = ModelDb.GetId(typeof(Ironclad));
    private static readonly ModelId SilentId = ModelDb.GetId(typeof(Silent));
    private static readonly ModelId DefectId = ModelDb.GetId(typeof(Defect));

    private sealed class Portrait(string normalPath, string bigPath)
    {
        private Texture2D? _normal;
        private Texture2D? _big;
        private bool _normalAttempted;
        private bool _bigAttempted;

        internal Texture2D? Get(bool big)
        {
            if (big)
            {
                if (!_bigAttempted)
                {
                    _bigAttempted = true;
                    _big = Load(bigPath);
                }
                return _big;
            }

            if (!_normalAttempted)
            {
                _normalAttempted = true;
                _normal = Load(normalPath);
            }
            return _normal;
        }

        private static Texture2D? Load(string path)
        {
            Texture2D? texture = ResourceLoader.Exists(path) ? ResourceLoader.Load<Texture2D>(path) : null;
            if (texture is null)
                MainFile.Logger.Error($"[Spire1] 原一代卡面资源不可用: {path}");
            return texture;
        }
    }

    private static Dictionary<Key, Portrait>? _portraits;
    private static bool _readAttempted;

    internal static bool IsSourceCharacter(CharacterModel? character) =>
        character is not null
        && SharedCardReuse.PureSts1PoolsAtRegistration
        && (character.Id.Equals(IroncladId) || character.Id.Equals(SilentId) || character.Id.Equals(DefectId));

    internal static Texture2D? Find(CharacterModel? character, CardModel? card, bool big)
    {
        if (!IsSourceCharacter(character) || card is null)
            return null;

        Dictionary<Key, Portrait>? rows = GetRows();
        if (rows is null)
            return null;

        var key = new Key(character!.Id, card.Id);
        return rows.TryGetValue(key, out Portrait? portrait) ? portrait.Get(big) : null;
    }

    private static Dictionary<Key, Portrait>? GetRows()
    {
        if (_readAttempted)
            return _portraits;
        _readAttempted = true;

        try
        {
            using Godot.FileAccess? file = Godot.FileAccess.Open(ManifestPath, Godot.FileAccess.ModeFlags.Read);
            if (file is null || file.GetLength() == 0 || file.GetLength() > MaximumManifestBytes)
                throw new InvalidOperationException("卡面清单缺失或大小无效");

            using JsonDocument document = JsonDocument.Parse(file.GetAsText());
            JsonElement root = document.RootElement;
            if (RequiredString(root, "schema") != "spire1-scoped-portraits/v1"
                || RequiredString(root, "effectiveMode") != "pure"
                || !root.TryGetProperty("entries", out JsonElement entries)
                || entries.ValueKind != JsonValueKind.Array
                || entries.GetArrayLength() == 0 || entries.GetArrayLength() > MaximumRows)
                throw new InvalidOperationException("卡面清单格式或条目数无效");

            var ids = new Dictionary<Type, ModelId>();
            var result = new Dictionary<Key, Portrait>(entries.GetArrayLength() * 2);
            foreach (JsonElement row in entries.EnumerateArray())
            {
                JsonElement source = row.GetProperty("character");
                JsonElement card = row.GetProperty("card");
                JsonElement legacy = row.GetProperty("legacy");
                JsonElement paths = row.GetProperty("paths");

                Type characterType = Resolve(RequiredString(source, "type"), typeof(CharacterModel));
                Type poolType = Resolve(RequiredString(source, "poolType"), typeof(CardPoolModel));
                if (!IsExpectedPool(characterType, poolType))
                    throw new InvalidOperationException("卡面角色与角色池不匹配");

                _ = RequiredString(legacy, "color");
                _ = RequiredString(legacy, "cardId");
                _ = RequiredString(legacy, "class");
                _ = RequiredString(legacy, "artKey");
                string normalPath = TexturePath(paths, "normal");
                string bigPath = TexturePath(paths, "big");
                var portrait = new Portrait(normalPath, bigPath);
                ModelId characterId = IdOf(characterType, ids);
                int models = 0;
                foreach (string property in new[] { "own", "native" })
                {
                    JsonElement typeValue = card.GetProperty(property);
                    if (typeValue.ValueKind == JsonValueKind.Null)
                        continue;
                    string typeName = typeValue.GetString() ?? throw new InvalidOperationException("卡牌类型为空");
                    Type cardType = Resolve(typeName, typeof(CardModel));
                    var key = new Key(characterId, IdOf(cardType, ids));
                    if (!result.TryAdd(key, portrait))
                        throw new InvalidOperationException($"卡面身份重复: {characterType.FullName} / {cardType.FullName}");
                    models++;
                }
                if (models == 0)
                    throw new InvalidOperationException("卡面条目没有真实模型类型");
            }

            _portraits = result;
            return result;
        }
        catch (Exception exception)
        {
            MainFile.Logger.Error($"[Spire1] 原一代卡面清单不可用: {exception.Message}");
            return null;
        }
    }

    private static ModelId IdOf(Type type, Dictionary<Type, ModelId> ids)
    {
        if (!ids.TryGetValue(type, out ModelId id))
        {
            id = ModelDb.GetId(type);
            ids.Add(type, id);
        }
        return id;
    }

    private static string RequiredString(JsonElement obj, string property)
    {
        string? value = obj.GetProperty(property).GetString();
        return !string.IsNullOrWhiteSpace(value) ? value : throw new InvalidOperationException($"清单缺少 {property}");
    }

    private static string TexturePath(JsonElement paths, string property)
    {
        string path = RequiredString(paths, property);
        if (!path.StartsWith(TextureRoot, StringComparison.Ordinal)
            || !path.EndsWith(".png", StringComparison.Ordinal)
            || path.Contains("..", StringComparison.Ordinal) || path.Contains('\\'))
            throw new InvalidOperationException($"卡面路径无效: {path}");
        return path;
    }

    private static Type Resolve(string fullName, Type baseType)
    {
        Type? type = typeof(Spire1PortraitSource).Assembly.GetType(fullName, false, false)
            ?? typeof(CardModel).Assembly.GetType(fullName, false, false);
        if (type is null || type.IsAbstract || !baseType.IsAssignableFrom(type))
            throw new InvalidOperationException($"卡面类型不可解析: {fullName}");
        return type;
    }

    private static bool IsExpectedPool(Type character, Type pool) =>
        (character == typeof(Ironclad) && pool == typeof(Spire1CardPool))
        || (character == typeof(Silent) && pool == typeof(SilentCardPool))
        || (character == typeof(Defect) && pool == typeof(DefectCardPool));
}
