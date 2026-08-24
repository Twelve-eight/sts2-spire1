// build_kb.mjs — SlayTheSpire v2.x (desktop-1.0.jar) knowledge-base extractor
// Reads ONLY the jar (never modified); writes JSON KB files next to this script.
// Usage: node build_kb.mjs [path-to-desktop-1.0.jar]
import fs from "fs";
import { inflateRawSync } from "zlib";
import { fileURLToPath } from "url";

const JARPATH = process.argv[2] ?? "G:/steam/steamapps/common/SlayTheSpire/desktop-1.0.jar";
const OUT = fileURLToPath(new URL(".", import.meta.url));

// ---------------- ZIP reader ----------------
class ZipList {
  constructor(path){
    const buf = fs.readFileSync(path); this.buf = buf;
    let i = buf.length - 22;
    while(i>=0 && buf.readUInt32LE(i)!==0x06054b50) i--;
    if(i<0) throw new Error("EOCD not found");
    const cnt = buf.readUInt16LE(i+10); let off = buf.readUInt32LE(i+16);
    this.byName = new Map();
    for(let k=0;k<cnt;k++){
      const nlen=buf.readUInt16LE(off+28), elen=buf.readUInt16LE(off+30), clen=buf.readUInt16LE(off+32);
      const csize=buf.readUInt32LE(off+20), lho=buf.readUInt32LE(off+42);
      this.byName.set(buf.toString("latin1",off+46,off+46+nlen), {lho,csize});
      off += 46+nlen+elen+clen;
    }
  }
}
function READ(z,name){
  const e=z.byName.get(name); if(!e) return null;
  const b=z.buf,lho=e.lho;
  const nl=b.readUInt16LE(lho+26), el=b.readUInt16LE(lho+28);
  const comp=b.subarray(lho+30+nl+el, lho+30+nl+el+e.csize);
  return b.readUInt16LE(lho+8)===0 ? Buffer.from(comp) : inflateRawSync(comp);
}

// ---------------- JVM class-file parser ----------------
const U16=(b,o)=>b.readUInt16BE(o), U32=(b,o)=>b.readUInt32BE(o);
function decodeMUTF8(b,o,len){
  const out=[];let i=0;
  while(i<len){const a=b[o+i];
    if(a<0x80&&a!==0){out.push(a);i++;}
    else if((a&0xE0)===0xC0){out.push(((a&0x1F)<<6)|(b[o+i+1]&0x3F));i+=2;}
    else if((a&0xF0)===0xE0){out.push(((a&0x0F)<<12)|((b[o+i+1]&0x3F)<<6)|(b[o+i+2]&0x3F));i+=3;}
    else throw new Error("utf8");}
  return Buffer.from(out).toString("utf8");
}
function parseCls(b){
  let o=8; const ncp=U16(b,o); o+=2; const cp=[null];
  for(let i=1;i<ncp;i++){
    const t=b[o++];
    if(t===7||t===8||t===16||t===19||t===20){cp.push({tag:t,i1:U16(b,o)});o+=2;}
    else if(t===15){cp.push({tag:t,b:b[o],i2:U16(b,o+1)});o+=3;}
    else if(t===9||t===10||t===11||t===12||t===17||t===18){cp.push({tag:t,i1:U16(b,o),i2:U16(b,o+2)});o+=4;}
    else if(t===3){cp.push({tag:t,num:b.readInt32BE(o)});o+=4;}
    else if(t===4){cp.push({tag:t,f:b.readFloatBE(o)});o+=4;}
    else if(t===5){cp.push({tag:t,num:[U32(b,o),U32(b,o+4)]});o+=8;i++;cp.push(null);}
    else if(t===6){cp.push({tag:t,d:b.readDoubleBE(o)});o+=8;i++;cp.push(null);}
    else if(t===1){const l=U16(b,o);cp.push({tag:t,s:decodeMUTF8(b,o+2,l)});o+=2+l;}
    else throw new Error("cptag"+t);
  }
  const S=i=>{const e=cp[i];return e&&e.tag===1?e.s:null;};
  const CN=i=>{const e=cp[i];return e?S(e.i1):null;};
  const acc=U16(b,o); o+=2;
  const thisCls=CN(U16(b,o)); o+=2; const supCls=CN(U16(b,o)); o+=2;
  const ifc=U16(b,o); o+=2+ifc*2;
  const rd=()=>{const c=U16(b,o);o+=2;const r=[];
    for(let k=0;k<c;k++){const ma=U16(b,o),nm=S(U16(b,o+2)),dsc=S(U16(b,o+4));o+=6;
      const ac=U16(b,o);o+=2;let co=-1,cl=0;
      for(let a=0;a<ac;a++){const an=S(U16(b,o));const al=U32(b,o+2);
        if(an==="Code"){cl=U32(b,o+10);co=o+14;} // max_stack(6) max_locals(8) code_length(10) -> code at 14
        o+=6+al;}
      r.push({acc:ma,nm,dsc,co,cl});}
    return r;};
  return {cp,S,CN,thisCls,supCls,acc,fields:rd(),methods:rd()};
}
function methodRef(c,i){const e=c.cp[i];const nt=c.cp[e.i2];return{c:c.CN(e.i1),n:c.S(nt.i1),d:c.S(nt.i2)};}
function ldcTok(c,i){const e=c.cp[i];if(!e)return{t:"?"};
  switch(e.tag){case 8:return{t:"str",v:c.S(e.i1)};case 3:return{t:"int",v:e.num};
    case 7:return{t:"class",v:c.S(e.i1)};default:return{t:"cp"+e.tag};}}
function argSlots(d){let i=1,s=0;
  while(d[i]!==')'){const ch=d[i];
    if(ch==='J'||ch==='D'){s+=2;i++;}
    else if(ch==='['){while(d[i]==='[')i++;if(d[i]==='L'){while(d[i]!==';')i++;i++;}else i++;s++;}
    else if(ch==='L'){while(d[i]!==';')i++;i++;s++;}
    else{i++;s++;}}
  return s;}
function returnsVoid(d){return d[d.indexOf(')')+1]==='V';}

function decode(code){
  const out=[];let pc=0;const n=code.length;
  while(pc<n){
    const op=code[pc];let len=-1;
    if(op===0xaa||op===0xab){ // tableswitch / lookupswitch
      const pad=(4-((pc+1)%4))%4;const p=pc+1+pad;
      if(op===0xaa){const lo=code.readInt32BE(p+4),hi=code.readInt32BE(p+8);
        if(hi<lo||hi-lo>65535)return{err:"badtsw@"+pc};len=1+pad+12+4*(hi-lo+1);}
      else{const np=code.readInt32BE(p+4);if(np<0||np>65535)return{err:"badlsw@"+pc};len=1+pad+8+8*np;}
    }
    else if(op===0xc4){len=(code[pc+1]===0x84||code[pc+1]===0xa9)?6:4;}       // wide
    else if([0xb9,0xba,0xc8,0xc9].includes(op))len=5;                          // iface/dynamic/goto_w/jsr_w
    else if(op===0x84||[0x11,0x13,0x14].includes(op))len=3;                    // iinc/sipush/ldc_w/ldc2_w
    else if((op>=0x15&&op<=0x19)||(op>=0x36&&op<=0x3a)||op===0xa9||op===0x10||op===0x12||op===0xbc)len=2;
    else if((op>=0x99&&op<=0xa8)||op===0xc6||op===0xc7)len=3;                  // branches
    else if([0xbb,0xbd,0xb2,0xb3,0xb4,0xb5,0xb6,0xb7,0xb8,0xc0,0xc1].includes(op))len=3;
    else if(op===0xc5)len=4;
    else len=1;
    if(len<1)return{err:"neglen"};
    if(pc+len>n)return{err:"overrun@"+pc};
    out.push({op,pc});pc+=len;
    if(out.length>50000)return{err:"toolong"};
  }
  return {ins:out};
}

// linear stack simulation; fires callbacks for invokes/putfields
function sim(clsBuf, cls, m, onInvoke){
  if(!m||m.co<0) return null;
  const code = clsBuf.subarray(m.co,m.co+m.cl);
  const D = decode(code);
  if(D.err) return {err:D.err};
  const st=[]; const push=t=>st.push(t); const popN=n=>n<=0?[]:st.splice(st.length-n,n);
  for(const I of D.ins){
    const op=I.op;
    if(op===0x2a)push({t:"this"});
    else if(op>=0x02&&op<=0x08)push({t:"int",v:op-0x03});
    else if(op===0x01)push({t:"null"});
    else if(op===0x10)push({t:"int",v:code.readInt8(I.pc+1)});
    else if(op===0x11)push({t:"int",v:code.readInt16BE(I.pc+1)});
    else if(op===0x12)push(ldcTok(cls,code[I.pc+1]));
    else if(op===0x13||op===0x14)push(ldcTok(cls,code.readUInt16BE(I.pc+1)));
    else if(op>=0x15&&op<=0x35)push({t:"loc"});
    else if(op>=0x36&&op<=0x41)popN(1);
    else if(op===0x57)popN(1);
    else if(op===0x59){if(st.length){const t=st[st.length-1];push({...t});}}
    else if([0x5a,0x5b,0x5d,0x5e].includes(op)){}
    else if(op===0x5c){if(st.length>1)push({...st[st.length-2]});}
    else if(op>=0x60&&op<=0x83){popN(2);push({t:"calc"});}
    else if(op>=0x84&&op<=0x98){}
    else if((op>=0x99&&op<=0xa4)||op===0xc6||op===0xc7)popN(1);
    else if(op>=0x9f&&op<=0xa6)popN(2);
    else if(op===0xaa||op===0xab)popN(1);
    else if(op>=0xac&&op<=0xb1)break;                    // xreturn
    else if(op===0xb2){const r=methodRef(cls,code.readUInt16BE(I.pc+1));push({t:"fld",c:r.c,n:r.n});}
    else if(op===0xb3)popN(1);
    else if(op===0xb4){popN(1);const r=methodRef(cls,code.readUInt16BE(I.pc+1));push({t:"fld",c:r.c,n:r.n});}
    else if(op===0xb5){const v=popN(1);popN(1);const r=methodRef(cls,code.readUInt16BE(I.pc+1));if(onInvoke)onInvoke("putfield",r,v[0]);}
    else if(op>=0xb6&&op<=0xb8){
      const r=methodRef(cls,code.readUInt16BE(I.pc+1));
      const stat=op===0xb8; const slots=argSlots(r.d)+(stat?0:1);
      let args=st.slice(Math.max(0,st.length-slots)); popN(slots);
      if(!stat)args=args.slice(1);
      if(onInvoke){if(r.n==="<init>")onInvoke("init",r,args);onInvoke(stat?"static":"invoke",r,args);}
      if(!returnsVoid(r.d))push({t:"ret"});}
    else if(op===0xb9||op===0xba){
      const r=methodRef(cls,code.readUInt16BE(I.pc+1));
      const slots=argSlots(r.d)+1;
      let args=st.slice(Math.max(0,st.length-slots)); popN(slots);
      args=args.slice(1);
      if(onInvoke){if(r.n==="<init>")onInvoke("init",r,args);onInvoke("invoke",r,args);}
      if(!returnsVoid(r.d))push({t:"ret"});}
    else if(op===0xbb)push({t:"new"});
    else if(op===0xbc||op===0xbd){popN(1);push({t:"arr"});}
    else if(op===0xbe)popN(1);
    else if(op===0xbf){popN(1);break;}
  }
  return {ok:true};
}

// ---------------- extraction ----------------
const jar=new ZipList(JARPATH);
const tokStr=a=>a&&a.t==="str"?a.v:null;
const tokInt=a=>a&&a.t==="int"?a.v:null;
const tokFld=a=>a&&a.t==="fld"?a.n:null;

function paramsOf(d){const p=[];let i=1;
  while(d[i]!==')'){
    if(d[i]==='L'){const j=d.indexOf(';',i);p.push(d.slice(i,j+1));i=j+1;}
    else if(d[i]==='['){while(d[i]==='[')i++;if(d[i]==='L'){i=d.indexOf(';',i)+1;}else i++;p.push("arr");}
    else{p.push(d[i]);i++;}}
  return p;}

function findSuper(clsBuf, cls, re){
  let hit=null;
  for(const m of cls.methods){
    if(m.nm!=="<init>")continue;
    sim(clsBuf,cls,m,(k,r,a)=>{if(k==="init"&&re.test(r.c)&&!hit)hit={d:r.d,args:a};});
    if(hit)break;
  }
  return hit;
}

const CARD_PKGS=["red","green","blue","purple","colorless","curses","status","tempCards","optionCards","deprecated"];
const cards={}; CARD_PKGS.forEach(p=>cards[p]=[]);
const warnings=[];

for(const p of CARD_PKGS){
  const names=[...jar.byName.keys()].filter(n=>n.startsWith(`com/megacrit/cardcrawl/cards/${p}/`)&&n.endsWith(".class")&&!n.includes("$"));
  for(const nm of names){
    const b=READ(jar,nm), c=parseCls(b);
    if(c.acc&0x0400){warnings.push(`skip abstract ${nm}`);continue;}
    const sup=findSuper(b,c,/cards\/AbstractCard$/);
    if(!sup){warnings.push(`no super ${nm}`);continue;}
    const ps=paramsOf(sup.d);
    let idx=null;
    if(ps.length===9&&ps[3]==="I")idx={id:0,cost:3,type:5,color:6,rarity:7,target:8};   // v2.x: id,name,img,cost,desc,type,color,rarity,target
    else if(ps.length===8&&ps[3]==="I")idx={id:0,cost:3,type:4,color:5,rarity:6,target:7};
    else{warnings.push(`unknown sig ${nm}: ${sup.d.slice(0,90)}`);continue;}
    const g=i=>sup.args[idx[i]];
    const ops=[];
    for(const m of c.methods){
      if(m.nm!=="upgrade")continue;
      sim(b,c,m,(k,r,a)=>{
        if(k==="putfield"&&["cost","baseCost","costForTurn"].includes(r.n))ops.push({op:"set."+r.n,val:tokInt(a[0])});
        if((k==="invoke"||k==="static")&&["upgradeCost","upgradeBaseCost"].includes(r.n))ops.push({op:r.n,val:tokInt(a.at(-1))});
      });
    }
    cards[p].push({cls:c.thisCls.split("/").pop(),pkg:p,id:tokStr(g("id")),cost:tokInt(g("cost")),
      type:tokFld(g("type")),color:tokFld(g("color")),rarity:tokFld(g("rarity")),target:tokFld(g("target")),upgradeOps:ops});
  }
}

const relicRows=[];
for(const nm of [...jar.byName.keys()].filter(n=>/^com\/megacrit\/cardcrawl\/relics\/[^$/]+\.class$/.test(n))){
  const b=READ(jar,nm), c=parseCls(b);
  if(c.acc&0x0400)continue;
  const sup=findSuper(b,c,/relics\/AbstractRelic$/);
  if(!sup){warnings.push(`relic no super ${nm}`);continue;}
  const strs=sup.args.filter(a=>a.t==="str").map(a=>a.v);
  const tierTok=sup.args.find(a=>a.t==="fld"&&/RelicTier$/.test(a.c||""));
  relicRows.push({cls:c.thisCls.split("/").pop(),id:strs[0]??null,tier:tierTok?tierTok.n:null});
}

const potionRows=[];
for(const nm of [...jar.byName.keys()].filter(n=>/^com\/megacrit\/cardcrawl\/potions\/[^$/]+\.class$/.test(n))){
  const b=READ(jar,nm), c=parseCls(b);
  if(c.acc&0x0400)continue;
  const sup=findSuper(b,c,/potions\/AbstractPotion$/);
  if(!sup){warnings.push(`potion no super ${nm}`);continue;}
  const strs=sup.args.filter(a=>a.t==="str").map(a=>a.v);
  const rarTok=sup.args.find(a=>a.t==="fld"&&/PotionRarity$/.test(a.c||""));
  potionRows.push({cls:c.thisCls.split("/").pop(),id:strs.at(-1)??null,rarity:rarTok?rarTok.n:null});
}

// ---------------- localization ----------------
const readJson=p=>JSON.parse(READ(jar,p).toString("utf8"));
const LOC={};
for(const lang of ["eng","zhs"]) LOC[lang]={
  cards:readJson(`localization/${lang}/cards.json`),
  relics:readJson(`localization/${lang}/relics.json`),
  potions:readJson(`localization/${lang}/potions.json`),
  events:readJson(`localization/${lang}/events.json`),
  keywords:readJson(`localization/${lang}/keywords.json`),
};

// ---------------- keyword scanner (official Game Dictionary names) ----------------
const kwDict={};
for(const lang of ["eng","zhs"]){
  const dict=LOC[lang].keywords["Game Dictionary"]||{};
  for(const [key,val] of Object.entries(dict)){
    kwDict[key]??={en:[],zh:[]};
    for(const n of val.NAMES??[]){
      if(/[\u4e00-\u9fff]/.test(n)){ if(lang==="zhs")kwDict[key].zh.push(n); }
      else kwDict[key][lang==="eng"?"en":"zh"].push(n.toLowerCase());
    }
  }
}
const esc=s=>s.replace(/[.*+?^${}()|[\]\\]/g,"\\$&");
const enMatchers=Object.entries(kwDict).map(([k,v])=>[k,
  v.en.map(n=>new RegExp(`(?<![A-Za-z])${esc(n)}(?![A-Za-z])`,"i"))]).filter(([,rs])=>rs.length);
const zhMatchers=Object.entries(kwDict).map(([k,v])=>[k,v.zh]).filter(([,ns])=>ns.length);

function scanKeywords(en,zh){
  const found=new Set();
  const cleanEn=(en??"").replace(/#[a-z]/g,"");
  const cleanZh=(zh??"").replace(/#[a-z]/g,"");
  for(const [k,rs] of enMatchers) if(rs.some(re=>re.test(cleanEn)))found.add(k);
  for(const [k,ns] of zhMatchers) if(ns.some(n=>cleanZh.includes(n)))found.add(k);
  return [...found].sort();
}

// ---------------- assemble ----------------
const joinDesc=a=>Array.isArray(a)?a.join("\n"):a??null;

function costUpgraded(cost,ops,cls){
  let cu=cost, src="unchanged";
  for(const op of ops){
    if(op.val==null)continue;
    if(op.op==="upgradeCost"){cu+=op.val;src=`upgradeCost(${op.val})`;}
    else if(op.op==="upgradeBaseCost"){cu=op.val;src=`upgradeBaseCost(${op.val})`;}
    else if(op.op.startsWith("set.")){cu=op.val;src=op.op;}
  }
  return {cost_upgraded:cu,cost_upgraded_source:src};
}

const stats={files:{},warnings};
const usedIds=new Set();

const deprecatedRows=[];
const pkgRows={};
for(const p of CARD_PKGS){
  const rows=[];
  for(const cd of cards[p]){
    const e=LOC.eng.cards[cd.id]??null, z=LOC.zhs.cards[cd.id]??null;
    const row={
      id:cd.id, class:cd.cls, color:cd.color,
      name_en:e?.NAME??null, name_zh:z?.NAME??null,
      type:cd.type, rarity:cd.rarity, cost:cd.cost,
      ...costUpgraded(cd.cost,cd.upgradeOps,cd.cls),
      target:cd.target,
      description_en:e?.DESCRIPTION??null, description_zh:z?.DESCRIPTION??null,
      upgraded_description_diff:(e?.UPGRADE_DESCRIPTION!=null||z?.UPGRADE_DESCRIPTION!=null)
        ?{en:e?.UPGRADE_DESCRIPTION??null, zh:z?.UPGRADE_DESCRIPTION??null} : null,
      keywords:scanKeywords(e?.DESCRIPTION,z?.DESCRIPTION),
    };
    // cards with no localization entry in this build are not player-reachable;
    // keep them, but only in the deprecated file so color files stay fully bilingual
    if(!row.name_en&&!row.name_zh&&p!=="deprecated"){
      row.note="no localization entry in this build";
      deprecatedRows.push(row);
      continue;
    }
    rows.push(row);
  }
  pkgRows[p]=rows;
}
for(const p of CARD_PKGS){
  const rows=(p==="deprecated"?deprecatedRows:[]).concat(pkgRows[p]);
  rows.sort((a,b)=>(a.class??"").localeCompare(b.class??""));
  const fname=`cards-${p}.json`;
  fs.writeFileSync(OUT+fname, JSON.stringify(rows,null,2)+"\n","utf8");
  stats.files[fname]=rows.length;
}

// relics
{
  const rows=relicRows.map(r=>{
    const e=LOC.eng.relics[r.id], z=LOC.zhs.relics[r.id];
    return {
      id:r.id, class:r.cls,
      name_en:e?.NAME??null, name_zh:z?.NAME??null, tier:r.tier,
      description_en:joinDesc(e?.DESCRIPTIONS), description_zh:joinDesc(z?.DESCRIPTIONS),
      flavor_en:e?.FLAVOR??null, flavor_zh:z?.FLAVOR??null,
    };
  }).sort((a,b)=>(a.class??"").localeCompare(b.class??""));
  fs.writeFileSync(OUT+"relics.json", JSON.stringify(rows,null,2)+"\n","utf8");
  stats.files["relics.json"]=rows.length;
}

// potions
{
  const rows=potionRows.map(p=>{
    const e=LOC.eng.potions[p.id], z=LOC.zhs.potions[p.id];
    return {
      id:p.id, class:p.cls,
      name_en:e?.NAME??null, name_zh:z?.NAME??null, rarity:p.rarity,
      description_en:joinDesc(e?.DESCRIPTIONS), description_zh:joinDesc(z?.DESCRIPTIONS),
      flavor_en:e?.FLAVOR??null, flavor_zh:z?.FLAVOR??null,
    };
  }).sort((a,b)=>(a.class??"").localeCompare(b.class??""));
  fs.writeFileSync(OUT+"potions.json", JSON.stringify(rows,null,2)+"\n","utf8");
  stats.files["potions.json"]=rows.length;
}

// events (from localizations; ids are event string keys)
{
  const rows=Object.keys(LOC.eng.events).sort().map(k=>{
    const e=LOC.eng.events[k], z=LOC.zhs.events[k]??{};
    return {
      id:k,
      name_en:Array.isArray(e.NAME)?e.NAME[0]:e.NAME??null,
      name_zh:Array.isArray(z.NAME)?z.NAME[0]:z.NAME??null,
      description_en:joinDesc(e.DESCRIPTIONS), description_zh:joinDesc(z.DESCRIPTIONS),
      options_en:Array.isArray(e.OPTIONS)?e.OPTIONS:null,
      options_zh:Array.isArray(z.OPTIONS)?z.OPTIONS:null,
    };
  });
  fs.writeFileSync(OUT+"events.json", JSON.stringify(rows,null,2)+"\n","utf8");
  stats.files["events.json"]=rows.length;
}

console.log(JSON.stringify(stats,null,2));
