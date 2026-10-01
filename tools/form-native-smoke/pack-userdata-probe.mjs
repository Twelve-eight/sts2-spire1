// 只打包两个文本资源, 不启动 Godot, 不安装依赖, 不执行子进程.
// 格式依据: godotengine/godot, tag 4.5.1-stable.
// core/io/file_access_pack.h:39-51: GDPC, V3, PACK_REL_FILEBASE.
// core/io/pck_packer.cpp:92-123,148-199,204-256: 头部/文件区/目录.
// core/io/file_access_pack.cpp:260-325: V3 读取链.
import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
import { createHash } from 'node:crypto';
import process from 'node:process';

const ROUND_ROOT = String.raw`G:\omp works\.tmp\form-playable-20260928-01a0e7ad`;
const SOURCE_ROOT = path.dirname(fileURLToPath(import.meta.url));
const FILE_NAMES = ['project.godot', 'userdata_probe.gd'];
const ALIGNMENT = 32;
const HEADER_SIZE = 6 * 4 + 2 * 8 + 16 * 4;
const align = (value, alignment) => Math.ceil(value / alignment) * alignment;
const hash = (algorithm, bytes) => createHash(algorithm).update(bytes).digest();

function canonicalGPath(value) {
  if (!/^[gG]:[\\/]/u.test(value)) throw new Error('输出必须是 G: 绝对路径.');
  const normalized = value.replaceAll('/', '\\');
  const parts = normalized.slice(3).split('\\');
  for (const part of parts) {
    if (!part || part === '.' || part === '..' || /[\x00-\x1f<>:"|?*~]/u.test(part)
        || /[. ]$/u.test(part) || /^(con|prn|aux|nul|com[1-9]|lpt[1-9])(?:\.|$)/iu.test(part)) {
      throw new Error('拒绝空目录段, 路径穿越, 设备名, ADS 或短路径别名.');
    }
  }
  return path.win32.normalize(normalized);
}

function isUnder(child, parent) {
  return child.toLowerCase().startsWith(parent.toLowerCase() + '\\');
}

function requireOrdinaryParent(directory) {
  // 不跟随 junction/symlink, 不创建目录. 调用者先建立独立的本轮 G: 目录.
  let current = 'G:\\';
  for (const part of directory.slice(3).split('\\')) {
    current = path.win32.join(current, part);
    const stat = fs.lstatSync(current);
    if (stat.isSymbolicLink() || !stat.isDirectory()) {
      throw new Error(`输出路径不是普通目录: ${current}`);
    }
    const real = fs.realpathSync.native(current).replace(/^\\\\\?\\/u, '');
    if (path.win32.normalize(real).toLowerCase() !== current.toLowerCase()) {
      throw new Error(`输出路径解析到其它位置: ${current}`);
    }
  }
}

function uint32(value) {
  const result = Buffer.alloc(4);
  result.writeUInt32LE(value);
  return result;
}

function uint64(value) {
  const result = Buffer.alloc(8);
  result.writeBigUInt64LE(BigInt(value));
  return result;
}

function makePack() {
  const fileBase = align(HEADER_SIZE, ALIGNMENT);
  const header = Buffer.alloc(fileBase);
  // 六个 uint32, 两个 uint64, 16 个保留 uint32; 其余为对齐零填充.
  [0x43504447, 3, 4, 5, 1, 2].forEach((value, index) => header.writeUInt32LE(value, index * 4));
  header.writeBigUInt64LE(BigInt(fileBase), 24);
  const payloads = [];
  const entries = [];
  let position = fileBase;

  for (const name of FILE_NAMES) {
    const source = path.join(SOURCE_ROOT, name);
    const stat = fs.lstatSync(source);
    if (!stat.isFile() || stat.isSymbolicLink() || stat.size === 0 || stat.size > 128 * 1024) {
      throw new Error(`预检资源必须是非空且不超过 128 KiB 的普通文件: ${name}`);
    }
    const bytes = fs.readFileSync(source);
    const text = bytes.toString('utf8');
    if (name === 'project.godot' && /^\s*\[(?:autoload|editor_plugins)\]/mu.test(text)) {
      throw new Error('预检 project.godot 不允许 autoload 或插件节.');
    }
    if (name === 'userdata_probe.gd' && !/^extends SceneTree\r?\n/u.test(text)) {
      throw new Error('预检脚本必须直接继承 SceneTree.');
    }
    entries.push({ name, offset: position - fileBase, size: bytes.length, md5: hash('md5', bytes) });
    payloads.push(bytes, Buffer.alloc(align(bytes.length, ALIGNMENT) - bytes.length));
    position += align(bytes.length, ALIGNMENT);
  }

  const directoryOffset = position;
  header.writeBigUInt64LE(BigInt(directoryOffset), 32);
  const directoryParts = [uint32(entries.length)];
  for (const entry of entries) {
    const utf8 = Buffer.from(entry.name, 'utf8');
    const paddedName = Buffer.alloc(align(utf8.length, 4));
    utf8.copy(paddedName);
    directoryParts.push(uint32(paddedName.length), paddedName, uint64(entry.offset),
      uint64(entry.size), entry.md5, uint32(0));
  }
  const directory = Buffer.concat(directoryParts);
  const bytes = Buffer.concat([header].concat(payloads, [directory]));
  if (bytes.length !== directoryOffset + directory.length) throw new Error('PCK 长度计算不一致.');
  return { bytes, fileBase, directoryOffset, entries };
}

try {
  if (process.platform !== 'win32') throw new Error('本工具仅接受此 Windows 工作区的 G: 输出.');
  const args = process.argv.slice(2);
  if (args.length !== 2 || args[0] !== '--output') {
    throw new Error('用法: node pack-userdata-probe.mjs --output <本轮 G: .tmp 下的新 .pck 文件>');
  }
  const output = canonicalGPath(args[1]);
  if (!isUnder(output, ROUND_ROOT) || path.win32.extname(output).toLowerCase() !== '.pck') {
    throw new Error('PCK 输出必须位于本轮 .tmp 之下, 且使用 .pck 扩展名.');
  }
  const parent = path.win32.dirname(output);
  requireOrdinaryParent(parent);
  const pack = makePack();
  requireOrdinaryParent(parent);
  // wx 拒绝覆盖已有文件, 包括已有链接; 失败时保留不完整文件供主会话核对.
  const fd = fs.openSync(output, 'wx');
  try {
    fs.writeFileSync(fd, pack.bytes);
    fs.fsyncSync(fd);
  } finally {
    fs.closeSync(fd);
  }
  process.stdout.write(JSON.stringify({
    status: 'PACKED_UNVERIFIED', output, format: 3, engine: '4.5.1',
    fileBase: pack.fileBase, directoryOffset: pack.directoryOffset,
    bytes: pack.bytes.length, sha256: hash('sha256', pack.bytes).toString('hex'),
    files: pack.entries.map(entry => ({ name: entry.name, offset: entry.offset, size: entry.size, md5: entry.md5.toString('hex') })),
  }) + '\n');
} catch (error) {
  process.stderr.write(JSON.stringify({ status: 'FAIL', error: String(error?.message ?? error) }) + '\n');
  process.exitCode = 2;
}
