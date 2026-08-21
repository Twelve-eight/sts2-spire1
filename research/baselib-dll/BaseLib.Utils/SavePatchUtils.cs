using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Text.Json.Serialization.Metadata;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Multiplayer.Serialization;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace BaseLib.Utils;

public class SavePatchUtils
{
	private static class SerializerDeserializerInfo<T>
	{
		public static Action<T, PacketWriter>? Serializer;

		public static Func<PacketReader, T>? Deserializer;
	}

	protected static readonly HashSet<Type> SupportedTypes;

	public static bool IsStoreTypeBaseSupported(Type t)
	{
		if (!SupportedTypes.Contains(t) && !t.IsEnum)
		{
			if (t.IsArray)
			{
				return t.GetElementType().IsEnum;
			}
			return false;
		}
		return true;
	}

	public static bool IsHolderTypeBaseSupported(Type t)
	{
		if (!t.IsAssignableTo(typeof(RelicModel)) && !t.IsAssignableTo(typeof(CardModel)) && !t.IsAssignableTo(typeof(EnchantmentModel)))
		{
			return t.IsAssignableTo(typeof(ModifierModel));
		}
		return true;
	}

	public static JsonPropertyInfoValues<PropType> QuickProps<ModifyingType, DeclaringType, PropType>(string propName, Func<ModifyingType, PropType?> getter, Action<ModifyingType, PropType?> setter)
	{
		return new JsonPropertyInfoValues<PropType>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(ModifyingType),
			Converter = null,
			Getter = (object obj) => getter((ModifyingType)obj),
			Setter = delegate(object obj, PropType? val)
			{
				setter((ModifyingType)obj, val);
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = propName,
			JsonPropertyName = propName,
			AttributeProviderFactory = () => typeof(DeclaringType).GetProperty(propName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(PropType), Array.Empty<Type>(), null)
		};
	}

	public static JsonPropertyInfoValues<PropType> QuickProps<ModifyingType, PropType>(string propName, Func<ModifyingType, PropType?> getter, Action<ModifyingType, PropType?> setter)
	{
		return new JsonPropertyInfoValues<PropType>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(ModifyingType),
			Converter = null,
			Getter = (object obj) => getter((ModifyingType)obj),
			Setter = delegate(object obj, PropType? val)
			{
				setter((ModifyingType)obj, val);
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = propName,
			JsonPropertyName = propName
		};
	}

	public static bool TryGetSerializerDeserializer<T>([NotNullWhen(true)] out Action<T, PacketWriter>? serializer, [NotNullWhen(true)] out Func<PacketReader, T>? deserializer)
	{
		serializer = SerializerDeserializerInfo<T>.Serializer;
		deserializer = SerializerDeserializerInfo<T>.Deserializer;
		if (serializer != null)
		{
			return deserializer != null;
		}
		return false;
	}

	static SavePatchUtils()
	{
		SupportedTypes = new HashSet<Type>
		{
			typeof(int),
			typeof(bool),
			typeof(string),
			typeof(ModelId),
			typeof(int[]),
			typeof(SerializableCard),
			typeof(SerializableCard[]),
			typeof(List<SerializableCard>)
		};
		SerializerDeserializerInfo<bool>.Serializer = delegate(bool val, PacketWriter writer)
		{
			writer.WriteBool(val);
		};
		SerializerDeserializerInfo<bool>.Deserializer = (PacketReader reader) => reader.ReadBool();
		SerializerDeserializerInfo<byte>.Serializer = delegate(byte val, PacketWriter writer)
		{
			writer.WriteByte(val, 8);
		};
		SerializerDeserializerInfo<byte>.Deserializer = (PacketReader reader) => reader.ReadByte(8);
		SerializerDeserializerInfo<short>.Serializer = delegate(short val, PacketWriter writer)
		{
			writer.WriteShort(val, 16);
		};
		SerializerDeserializerInfo<short>.Deserializer = (PacketReader reader) => reader.ReadShort(16);
		SerializerDeserializerInfo<int>.Serializer = delegate(int val, PacketWriter writer)
		{
			writer.WriteInt(val, 32);
		};
		SerializerDeserializerInfo<int>.Deserializer = (PacketReader reader) => reader.ReadInt(32);
		SerializerDeserializerInfo<long>.Serializer = delegate(long val, PacketWriter writer)
		{
			writer.WriteLong(val, 64);
		};
		SerializerDeserializerInfo<long>.Deserializer = (PacketReader reader) => reader.ReadLong(64);
		SerializerDeserializerInfo<ushort>.Serializer = delegate(ushort val, PacketWriter writer)
		{
			writer.WriteUShort(val, 16);
		};
		SerializerDeserializerInfo<ushort>.Deserializer = (PacketReader reader) => reader.ReadUShort(16);
		SerializerDeserializerInfo<uint>.Serializer = delegate(uint val, PacketWriter writer)
		{
			writer.WriteUInt(val, 32);
		};
		SerializerDeserializerInfo<uint>.Deserializer = (PacketReader reader) => reader.ReadUInt(32);
		SerializerDeserializerInfo<ulong>.Serializer = delegate(ulong val, PacketWriter writer)
		{
			writer.WriteULong(val, 64);
		};
		SerializerDeserializerInfo<ulong>.Deserializer = (PacketReader reader) => reader.ReadULong(64);
		SerializerDeserializerInfo<float>.Serializer = delegate(float val, PacketWriter writer)
		{
			writer.WriteFloat(val, (QuantizeParams?)null);
		};
		SerializerDeserializerInfo<float>.Deserializer = (PacketReader reader) => reader.ReadFloat((QuantizeParams?)null);
		SerializerDeserializerInfo<double>.Serializer = delegate(double val, PacketWriter writer)
		{
			writer.WriteDouble(val);
		};
		SerializerDeserializerInfo<double>.Deserializer = (PacketReader reader) => reader.ReadDouble();
		SerializerDeserializerInfo<decimal>.Serializer = delegate(decimal val, PacketWriter writer)
		{
			writer.WriteDouble((double)val);
		};
		SerializerDeserializerInfo<decimal>.Deserializer = (PacketReader reader) => (decimal)reader.ReadDouble();
		SerializerDeserializerInfo<string>.Serializer = delegate(string val, PacketWriter writer)
		{
			writer.WriteString(val);
		};
		SerializerDeserializerInfo<string>.Deserializer = (PacketReader reader) => reader.ReadString();
		SerializerDeserializerInfo<ModelId>.Serializer = delegate(ModelId val, PacketWriter writer)
		{
			PacketWriterExtensions.WriteFullModelId(writer, val);
		};
		SerializerDeserializerInfo<ModelId>.Deserializer = (PacketReader reader) => PacketReaderExtensions.ReadFullModelId(reader);
	}
}
