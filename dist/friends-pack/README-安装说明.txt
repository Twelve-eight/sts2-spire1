【StS1 回归层 + AFTP 联机修复版】朋友安装包（2026-08-27）

一、内容
  mods/Spire1/          —— StS1 内容层（铁甲战士/静默猎手/故障机器人三角色全启用）
                           306 张一代卡、一代遗物/药水/事件、可选一代地牢
  mods/ActsFromThePast/ —— AFTP 一代楼层（fork 修复版）
                           含联机断线修复：经典粘液标记丢失、RebalancedMode 单端分歧
                           注意：dll 为修复版，资源包(pck)与创意工坊 1.0.5 相同

二、前置要求
  1. Slay the Spire 2（公开测试分支 v0.111.x）
  2. 创意工坊订阅 BaseLib（版本 >= 3.4.5）——必装，其余无要求

三、安装
  1. 解压本压缩包到游戏根目录（与 SlayTheSpire2.exe 同级），
     解压后应出现 mods/Spire1/ 与 mods/ActsFromThePast/ 两个文件夹。
  2. 启动游戏 → Mods 列表 → 确认 Spire1 与 Acts from the Past 均已启用。
  3. 若你已订阅创意工坊的 Acts from the Past：
     无需退订——本地版会自动优先（启动日志可见 "Disabling the Steam workshop version"）。

四、联机须知（重要）
  房主与客人**双方都要装本包的 ActsFromThePast**，断线修复才生效：
  修复需要两端行为一致，只装一端无效。
  已修复的断线场景：打出史莱姆粘液卡（消耗/抽牌分歧）、复制机事件（祈祷/跪下分歧）。

五、文件清单校验（md5）
  Spire1/Spire1.dll          应与房主一致（发包时随附校验值）
  ActsFromThePast.dll        应与房主一致（fork 修复版）
  ActsFromThePast.pck        110,327,596 字节（与工坊版相同，勿用小体积版本替换）

六、默认配置
  三代角色全启用（character.txt=all）。如需单角色体验，把该文件内容改为
  ironclad / silent / defect 之一即可（dll/pck 不变）。

附：发包时校验值
  mods/Spire1/Spire1.dll  5e0083d5acc68c7a31357c228ea7de73
  mods/Spire1/Spire1.pck  690dce840f07d061db6eecd6dabfe21e
  mods/ActsFromThePast/ActsFromThePast.dll  c2c49c620fce7c5f94d3ddba6046cbd5
  mods/ActsFromThePast/ActsFromThePast.pck  ba60133a597bf7b80bddcccdd4c493db
