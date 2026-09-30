import fs from 'node:fs';
import path from 'node:path';
import solc from 'solc';
import { fileURLToPath } from 'node:url';
const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..');
const source = fs.readFileSync(path.join(root, 'contracts/FennecPetNFT.sol'), 'utf8');
const input = { language:'Solidity', sources:{'FennecPetNFT.sol':{content:source}},
  settings:{optimizer:{enabled:true,runs:200},evmVersion:'shanghai',
    outputSelection:{'*':{'*':['abi','evm.bytecode.object']}}}};
const output = JSON.parse(solc.compile(JSON.stringify(input), {import:name => {
  const target = path.resolve(root,'node_modules',name);
  if (!target.startsWith(path.join(root,'node_modules')+path.sep)) return {error:'Invalid import'};
  try { return {contents:fs.readFileSync(target,'utf8')}; } catch { return {error:'Missing '+name}; }
}}));
for(const e of output.errors || []) console.log(e.formattedMessage);
if((output.errors || []).some(e=>e.severity==='error')) process.exit(1);
const c=output.contracts['FennecPetNFT.sol'].FennecPetNFT;
fs.writeFileSync(path.join(root,'web/contract.json'),JSON.stringify({abi:c.abi,bytecode:'0x'+c.evm.bytecode.object},null,2));
fs.copyFileSync(path.join(root,'node_modules/ethers/dist/ethers.min.js'),path.join(root,'web/ethers.min.js'));
// A single-file Remix source pinned to exactly the dependencies used by local tests.
const seen=new Set();
function flatten(name,content) {
  if(seen.has(name)) return '';
  seen.add(name);
  let dependencies='';
  content=content.replace(/import\s+(?:[^;]*?from\s+)?["']([^"']+)["'];/g,(_,imp)=>{
    const resolved=imp.startsWith('@')?imp:path.posix.normalize(path.posix.join(path.posix.dirname(name),imp));
    dependencies+=flatten(resolved,fs.readFileSync(path.join(root,'node_modules',resolved),'utf8'));
    return '';
  });
  return dependencies+'\n// '+name+'\n'+content.replace(/\/\/ SPDX-License-Identifier:[^\n]*/g,'').replace(/pragma solidity[^;]+;/g,'')+'\n';
}
fs.writeFileSync(path.join(root,'FennecPetNFT_REMIX.sol'),'// SPDX-License-Identifier: MIT\npragma solidity 0.8.30;\n'+flatten('FennecPetNFT.sol',source));
console.log('Compiled NFT contract, copied browser library, generated single-file Remix source.');
