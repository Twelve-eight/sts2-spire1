# 机制确证: 死神形态(ReaperForm)/灾厄(DoomPower) 触发判据

来源: StS2 官方反编译转储 G:\omp works\Sts\sts2-spire1\.tmp\dllsrc\ (权威信源, 2026-09-26 核对)
状态: 已确认 (逐文件源码引用)

## 核心判据

DoomPower(灾厄) 的施加发生在 ReaperFormPower.AfterDamageGiven, 需同时满足三条:
1. dealer == Owner 或 dealer.PetOwner?.Creature == Owner (造成者是玩家本人, 或玩家宠物)
2. props.IsPoweredAttack() == true
3. result.TotalDamage > 0
施加层数 = result.TotalDamage * Amount 的 DoomPower, 施加到 target.

## 源码引用

- ReaperFormPower.cs (Models.Powers): AfterDamageGiven 中上述三条件 + PowerCmd.Apply<DoomPower>(target, result.TotalDamage*Amount)
- ValuePropExtensions.cs (ValueProps): IsPoweredAttack() = props.HasFlag(Move) && !props.HasFlag(Unpowered); 无 Move 直接 false
- CreatureCmd.cs:412 (Commands): 所有经 CreatureCmd.Damage 的伤害(含中毒)都触发 Hook.AfterDamageGiven -> 分界线是 IsPoweredAttack, 不是"走没走伤害管线"
- PoisonPower.cs (Models.Powers): Trigger() 用 CreatureCmd.Damage(..., ValueProp.Unblockable|ValueProp.Unpowered, ...) -> 带 Unpowered/无 Move -> IsPoweredAttack=false
- OstyCmd.cs (Commands): 奥斯提是玩家宠物, AddPet<Osty>(summoner), PetOwner==summoner
- Osty.cs (Models.Monsters): move state = NOTHING_MOVE, 奥斯提本体不主动攻击; 其伤害仅来自玩家打出的 OstyAttack 卡
- Poke.cs / SicEm.cs (Models.Cards): OstyAttack 卡用 new OstyDamageVar(N, ValueProp.Move), 走 DamageCmd.Attack(...).FromOsty(osty,...)
- AttackCommand.cs:127,238,669 (Commands.Builders): DamageProps 默认 = ValueProp.Move; FromOsty 设 Attacker=osty(即 dealer); 最终 CreatureCmd.Damage(props=Move, dealer=osty)
- DieForYouPower.cs (Models.Powers): ModifyUnblockedDamageTarget 仅在 IsPoweredAttack 时把伤害转移到奥斯提替玩家挡刀 —— 是挡伤转移, 非奥斯提对敌造成伤害

## 逐途径结论 (给不给灾厄)

| 途径 | 给灾厄 | 依据 |
|---|---|---|
| 使敌人失去生命(lose HP) | 否 | 带 Unpowered/无 Move, IsPoweredAttack=false |
| 中毒 | 否 | PoisonPower 用 Unblockable|Unpowered (虽触发 AfterDamageGiven 但被判据挡掉) |
| 奥斯提的攻击 | 是 | 奥斯提是玩家 pet, 攻击卡 props=Move -> IsPoweredAttack=true; 唯一"非玩家本人却给灾厄"的途径 |
| 奥斯提失血导致敌人失去生命 | 否 | DieForYouPower 是挡刀转移, 非奥斯提对敌造成伤害, 不触发对敌 AfterDamageGiven |

## 纠错记录 (供知识库避免复犯)
- 曾误记死神=回血/只认玩家本人 -> 错, 是给灾厄且含玩家宠物.
- 曾以"loseHP vs damage 管线"为分界线 -> 错. 中毒也走 CreatureCmd.Damage 并触发 AfterDamageGiven; 真正分界线是 IsPoweredAttack 的 ValueProp(Move/Unpowered) 标志.
