# friends-pack 重建待办 (2026-09-12 核实)

现状 (dist/friends-pack/ 与 dist/friends-pack.zip, 构建于 2026-08-29):

| 组件 | 包内版本 | 当前版本 | 包内 dll MD5 | 当前 dll MD5 |
|---|---|---|---|---|
| Spire1 | 0.9.2 | 1.1.0 | - | - |
| ActsFromThePast | 1.0.5 | 1.0.6 | 317ad0345f64fccef14d727ddbc46563 | 58310ad9a6ccd0e8787eaef9f6b762df |

两项都落后:
1. Spire1 落后 0.9.2 -> 1.1.0 (v1.0.0 正式发布 + v1.1.0 的 AFTP 生态整合: 移除 46 个
   重复事件与孤立卡/遗物, 保留 6 个独有事件).
2. AFTP fork 落后 1.0.5 -> 1.0.6: 包内 dll 缺 family-C / family-D 的联机极性修复
   (DarvOnlyInLegacyActs / LegacyEnemiesGiveClassicSlimed / allow-flags 在 MP 下
   必须返回 true), 这些是 2026-09-01 之后的提交.

重建步骤:
1. 重新构建 Spire1 (workshop/content/Spire1 已是 v1.1.0 payload) 与 AFTP fork
   (G:/omp works/aftp-ActsFromThePast -> G:/omp works/aftp-stage).
2. 用两者覆盖 dist/friends-pack/mods/ 下的对应目录 (AFTP 的 pck 必须用完整工坊
   资源文件, 不能用本地化精简 pck).
3. 更新 README-安装说明.txt 的版本/日期/内容描述.
4. 重新打包 dist/friends-pack.zip.

注意: 包内 Spire1.json 的 name 字段是 "Spire1"(不是工坊版的
"Spire1: StS1 Characters"), 且 description 描述的是"由 character.txt 决定角色"
的旧打包方式 - 重建时需确认这两点是否仍是有意为之.
