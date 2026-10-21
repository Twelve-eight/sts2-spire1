# docs/ — 公开文档索引 / Public documentation index

本目录只放**随仓库分发的公开文档**，面向贡献者与未来的维护者。
开发日志、审查报告、历史交接、带日期的设计契约与轮次证据属于内部工程记录，
**不随本仓库分发**；需要追溯这些材料请联系维护者。

## 文档 / Documents

| File | Contents |
|---|---|
| `BaseLib-API.md` | BaseLib 接口参考（StS2 v0.111.0；shipped v3.3.5 二进制与 v3.4.5 源码），用 `SHIPPED` / `SOURCE-ONLY` / `ENGINE` 标记可用性，并为每条签名标注反编译或源码出处；写代码前先查这里。 |
| `RELEASE-GATES-CHECKLIST.md` | 发布前门禁清单：AssemblyRef、manifest 一致性与 TypeDef 条件编译黑名单三道结构性门禁的运行、解读和失败处理，全部解析编译后的 ECMA-335 元数据，工具在 `tools/build-gates/`。 |
| `RELEASE-PACKAGE-CONTRACT-20261003.md` | 发布包契约：从源码 allowlist 在隔离 staging 中重建，经 PCK 逐项校验后才允许显式 `-Promote`；定义正式 payload、资产保留/排除边界与未验证范围。 |

## 相关公开文档 / Related documents

- `../README.md` — 玩家向说明：安装、依赖与功能列表。
- `../CONTRIBUTING.md` — 构建、发布门禁、代码与提交约定。
- `../DEVELOP.md` — 设计目标、契约与里程碑。
- `../CHANGELOG.md` — 对外变更记录。

## 维护 / Maintenance

- 新增公开文档放进本目录，并在上表登记；文件名不要命中 `.gitignore` 中登记的内部文档模式。
- 内部文档（`DEVLOG.md`、`DEVELOP-*.md`、`HANDOFF-*`、`CHECKPOINT-*`、`reports/`、`historical/`、`evidence/` 等）在内部仓维护，不要提交到本仓库。
- 文档与代码冲突时以代码为准，然后在同一 PR 内修正文档。
