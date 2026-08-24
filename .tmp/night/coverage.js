// 权威覆盖计算器 — 用法: node coverage.js
// 口径: 池归属按 [Pool] 属性继承链解析(Spire1Card基类=Spire1CardPool);
// 已验证 = 任一 autoslay 日志出现 "Playing <id>"; 官方复用卡 id = 类名蛇形大写。
const fs=require('fs'),path=require('path');
const R='G:/omp works/sts2-spire1';
const SPEC={'D':'Damage','CD':'CalculatedDamage','B':'Block','CB':'CalculatedBlock','C':'Cards','E':'Energy','H':'Heal'};
const DEF=JSON.parse(fs.readFileSync(R+'/.tmp/var-default-names.json','utf8'));
DEF['ScryVar']='Scry';
const cardDir=R+'/mod/Spire1Code/Cards';
function snake(n){const sp={JAX:'J_A_X',FTL:'F_T_L',CreativeAI:'CREATIVE_A_I'};return sp[n]||n.replace(/([a-z0-9])([A-Z])/g,'$1_$2').replace(/([A-Z]+)([A-Z][a-z])/g,'$1_$2').toUpperCase();}
const zhs=fs.readFileSync(R+'/mod/Spire1/localization/zhs/cards.json','utf8');
// 1) 收集类行
const rows=[];
for(const f of fs.readdirSync(cardDir).filter(f=>f.endsWith('.cs'))){
  const t=fs.readFileSync(path.join(cardDir,f),'utf8');
  const cls=(t.match(/^\s*(?:public |internal |abstract |sealed |partial )*\s*class\s+(\w+)/m)||[])[1];
  if(!cls)continue;
  const poolM=t.match(/\[Pool\(typeof\((\w+)\)\)\]/);
  const inh=t.match(/:\s*(\w+)\s*\(/);
  const rar=(t.match(/CardRarity\.(\w+)/)||[])[1];
  rows.push({f,cls,pool:poolM&&poolM[1],base:inh&&inh[1],rarity:rar});
}
function poolOf(r,seen=new Set()){
  if(!r||seen.has(r.cls))return null;
  seen.add(r.cls);
  if(r.pool)return r.pool;
  return poolOf(rows.find(x=>x.cls===r.base),seen);
}
const PLAY_POOL={'Spire1CardPool':'SPIRE1-IRONCLAD','SilentCardPool':'SPIRE1-SILENT','DefectCardPool':'SPIRE1-DEFECT','WatcherCardPool':'SPIRE1-WATCHER'};
const REUSE={
 'Spire1CardPool':['Anger','Armaments','BodySlam','Havoc','Headbutt','IronWave','PommelStrike','ShrugItOff','ThunderClap','TwinStrike'],
 'SilentCardPool':['Backflip','CloakAndDagger','DaggerSpray','DaggerThrow','DeadlyPoison','Deflect','DodgeAndRoll','PiercingWail','Prepared','Slice'],
 'DefectCardPool':['BallLightning','BeamCell','ColdSnap','CompileDriver','Coolheaded','GoForTheEyes','Hologram','Leap','SweepingBeam','Turbo','BootSequence','Capacitor','Chaos','DoubleEnergy','Equilibrium','Loop','Overclock','Scrape','Skim','WhiteNoise','Buffer','EchoForm','MachineLearning','MeteorStrike','Rainbow','Reboot'],
};
// 2) played 集合
let played=new Set();
for(const l of fs.readdirSync(R+'/.tmp/p1-smoke')){
  if(!/^autoslay.*\.log$/.test(l))continue;
  const t=fs.readFileSync(R+'/.tmp/p1-smoke/'+l,'utf8');
  for(const m of t.matchAll(/Playing (\S+)/g))played.add(m[1]);
}
// 3) 矩阵
let out='';
const DISPLAY={'Spire1CardPool':'SPIRE1-IRONCLAD','SilentCardPool':'SPIRE1-SILENT','DefectCardPool':'SPIRE1-DEFECT','WatcherCardPool':'SPIRE1-WATCHER'};
for(const [poolName,key] of Object.entries(DISPLAY)){
  let total=0,done=0,missObjs=[];
  for(const r of rows){
    if(r.rarity==='Token')continue;
    if(poolOf(r)!==poolName)continue;
    if(!r.rarity)continue; // 抽象基类
    total++;
    const id='SPIRE1-'+snake(r.cls);
    const vid=snake(r.cls); // 复用通道：我方池内与官方同实现的牌，运行日志落原版 id（如 STRIKE_IRONCLAD）
    if(played.has(id)||played.has(vid))done++;else missObjs.push({id,label:r.cls});
  }
  for(const n of (REUSE[poolName]||[])){
    if(key==='SPIRE1-SILENT'&&n==='BladeDance')continue;
    total++;
    const sid=n.replace(/([a-z0-9])([A-Z])/g,'$1_$2').toUpperCase();
    if(played.has(sid))done++;else missObjs.push({id:n,label:'(官方)'+n});
  }
  fs.writeFileSync(R+'/.tmp/night/queue-'+key+'.txt',missObjs.map(m=>m.id).join('\n'));
  out+=`◆ ${key}: ${done}/${total}${missObjs.length?'  缺:'+missObjs.map(m=>m.label).join(', '):'  ✅全覆盖'}\n`;
}
out+='队列文件已按池写入。';
console.log(out);
fs.writeFileSync(R+'/.tmp/night/COVERAGE.md',out);
