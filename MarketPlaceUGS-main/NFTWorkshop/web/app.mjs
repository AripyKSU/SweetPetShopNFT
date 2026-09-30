import {BrowserProvider,Contract,ContractFactory,hexlify,randomBytes,keccak256,isAddress,ZeroAddress} from './ethers.min.js';
const artifact=await (await fetch('/contract.json')).json();
const $=id=>document.getElementById(id);
let provider, signer, busy=false;
const log=t=>{$('status').textContent=t;};
const address=(v,label='공개 주소')=>{
 if(/^0x[0-9a-fA-F]{64}$/.test(v))throw Error(label+' 칸에 64자리 거래 해시를 입력했습니다. 계약 주소는 0x로 시작하는 40자리 주소입니다.');
 if(!isAddress(v)||v===ZeroAddress)throw Error(label+'를 확인하세요. 0x로 시작하는 40자리 주소가 필요합니다.');
 return v;
};
const publicUri=v=>{
 const uri=v.trim();
 if(/^ipfs:\/\/[a-zA-Z0-9]+(?:\/[^\s]*)?$/.test(uri)||/^https:\/\/[^\s]+$/.test(uri))return uri;
 throw Error('공개 ipfs:// 또는 https:// 주소를 입력하세요. 로컬 주소와 data: URI는 사용할 수 없습니다.');
};
const gateway=uri=>uri.startsWith('ipfs://')?'https://gateway.pinata.cloud/ipfs/'+uri.slice(7):uri;
const inlineMetadata=image=>{
 const data={name:'Starlight Fennec Pet NFT',description:'A starlight fennec pet on Sepolia.',image:gateway(image),attributes:[{trait_type:'Pet',value:'Fennec Fox'},{trait_type:'Rarity',value:'Mythic'}]};
 return 'data:application/json;base64,'+btoa(JSON.stringify(data));
};
async function metadata(uri) {
 let data;
 if(uri.startsWith('data:application/json;base64,'))data=JSON.parse(atob(uri.slice('data:application/json;base64,'.length)));
 else {
  const response=await fetch(gateway(publicUri(uri)),{cache:'no-store'});
  if(!response.ok)throw Error('메타데이터를 읽을 수 없습니다: HTTP '+response.status);
  data=await response.json();
 }
 if(!data||!data.name||!data.image)throw Error('메타데이터에 name 또는 image가 없습니다.');
 publicUri(data.image);
 await new Promise((resolve,reject)=>{const image=new Image();image.onload=resolve;image.onerror=()=>reject(Error('메타데이터 image를 표시할 수 없습니다.'));image.src=gateway(data.image);});
 return data;
}
function parse(text) {
 const parts=text.trim().split('|');
 if(parts.length!==5||parts[0]!=='MSW1'||parts[1]!=='11155111'||!/^0x[0-9a-fA-F]{64}$/.test(parts[4]))throw Error('쿠폰 전체를 다시 붙여넣으세요.');
 address(parts[2]);address(parts[3]);return {contract:parts[2],recipient:parts[3],secret:parts[4]};
}
async function session() {
 if(!window.ethereum)throw Error('PC Chrome의 MetaMask 확장 프로그램이 필요합니다.');
 let chain=await window.ethereum.request({method:'eth_chainId'});
 if(BigInt(chain)!==11155111n) {
  await window.ethereum.request({method:'wallet_switchEthereumChain',params:[{chainId:'0xaa36a7'}]});
  chain=await window.ethereum.request({method:'eth_chainId'});
  if(BigInt(chain)!==11155111n)throw Error('웹사이트 연결 네트워크가 Sepolia로 변경되지 않았습니다. 현재 chainId: '+chain);
 }
 provider=new BrowserProvider(window.ethereum);
 signer=await provider.getSigner();
 $('account').textContent='Sepolia / '+await signer.getAddress();
 return signer;
}
async function collection(admin=false) {
 await session();const a=address($('contract').value.trim(),'NFT 계약 주소');
 if(await provider.getCode(a)==='0x')throw Error('이 주소에 계약이 없습니다.');
 const c=new Contract(a,artifact.abi,signer);
 if(await c.symbol()!=='FENNEC')throw Error('페넥여우 계약 주소를 확인하세요.');
 if(admin&&(await c.owner()).toLowerCase()!==(await signer.getAddress()).toLowerCase())throw Error('배포한 관리자 계정으로 전환하세요.');
 localStorage.setItem('fennec-contract',a);return c;
}
async function wait(tx) {
 localStorage.setItem('mythic-last-tx',tx.hash);
 log('전송됨. 승인 대기 중입니다. 다시 보내지 마세요.\n거래 해시: '+tx.hash+'\nhttps://sepolia.etherscan.io/tx/'+tx.hash);
 const receipt=await tx.wait(3);
 if(receipt.status!==1)throw Error('트랜잭션 실패. 거래 해시를 보관하세요.');
 return receipt;
}
function bind(id,fn) {$(id).onclick=async()=>{
 if(busy)return;busy=true;document.querySelectorAll('button').forEach(x=>x.disabled=true);
 try{await fn();}catch(e){log('확인 필요: '+(e.reason||e.shortMessage||e.message)+'\n마지막 거래: '+(localStorage.getItem('mythic-last-tx')||'없음')+'\n전송 후 오류라면 재전송 전에 쿠폰 상태 조회를 누르세요.');}
 finally{busy=false;document.querySelectorAll('button').forEach(x=>x.disabled=false);}
};}
$('contract').value=localStorage.getItem('fennec-contract')||'';
bind('connect',async()=>{await window.ethereum.request({method:'eth_requestAccounts'});await session();log('연결 완료');});
bind('checkImage',async()=>{
 const uri=publicUri($('imageUri').value);
 await new Promise((resolve,reject)=>{const image=new Image();image.onload=resolve;image.onerror=()=>reject(Error('공개 이미지 로드 실패'));image.src=gateway(uri);});
 log('공개 이미지가 브라우저에서 열립니다: '+uri);
});
bind('useInlineMetadata',async()=>{
 const image=publicUri($('imageUri').value);
 $('uri').value=inlineMetadata(image);
 const data=await metadata($('uri').value);
 log('JSON 업로드 없이 메타데이터를 계약에 내장할 준비가 됐습니다.\nimage: '+data.image);
});
bind('downloadMetadata',async()=>{
 const image=publicUri($('imageUri').value);
 const data={name:'Starlight Fennec Pet NFT',description:'A starlight fennec pet on Sepolia.',image:gateway(image),attributes:[{trait_type:'Pet',value:'Fennec Fox'},{trait_type:'Rarity',value:'Mythic'}]};
 const blob=new Blob([JSON.stringify(data,null,2)+'\n'],{type:'application/json'}),link=document.createElement('a');
 link.href=URL.createObjectURL(blob);link.download='fennec-nft-metadata.json';link.click();setTimeout(()=>URL.revokeObjectURL(link.href),1000);
 log('메타데이터 JSON을 다운로드했습니다. 공개 저장소에 업로드한 뒤 URL을 입력하세요.');
});
bind('checkMetadata',async()=>{const data=await metadata($('uri').value);log('메타데이터와 이미지 확인 완료\nname: '+data.name+'\nimage: '+data.image);});
bind('deploy',async()=>{
 await session();const uri=$('uri').value.trim(), n=Number($('supply').value);
 if(!uri.startsWith('data:application/json;base64,'))publicUri(uri);
 if(!Number.isSafeInteger(n)||n<1||n>100000)throw Error('최대 발행 개수를 확인하세요.');
 const data=await metadata(uri);
 if(publicUri(data.image)!==gateway(publicUri($('imageUri').value)))throw Error('메타데이터 image가 입력한 페넥여우 PNG 주소와 다릅니다.');
 const factory=new ContractFactory(artifact.abi,artifact.bytecode,signer);
 const c=await factory.deploy(await signer.getAddress(),uri,n);
 $('contract').value=await c.getAddress();localStorage.setItem('fennec-contract',$('contract').value);
 await wait(c.deploymentTransaction());log('배포 완료. 계약 주소를 저장하세요:\n'+$('contract').value);
});
bind('verifyToken',async()=>{
 const c=await collection(),id=$('tokenId').value.trim();
 if(!/^[1-9][0-9]*$/.test(id))throw Error('발급된 tokenId를 입력하세요.');
 const uri=await c.tokenURI(id),data=await metadata(uri);
 log('온체인 tokenURI: '+uri+'\nname: '+data.name+'\nimage: '+data.image+'\nMetaMask에서 같은 이미지가 보이는지는 해당 지갑에서 확인하세요.');
});
bind('issue',async()=>{
 const c=await collection(true), recipient=address($('recipient').value.trim(),'학생 수령 지갑 주소'), days=Number($('days').value);
 if(!Number.isInteger(days)||days<1||days>365)throw Error('기간은 1~365일입니다.');
 const secret=hexlify(randomBytes(32)), code=['MSW1','11155111',await c.getAddress(),recipient,secret].join('|');
 // Keep the draft even if approval is rejected or the browser closes.
 $('issued').value=code;localStorage.setItem('mythic-last-coupon',code);
 const block=await provider.getBlock('latest');
 await wait(await c.registerCoupon(keccak256(secret),recipient,block.timestamp+days*86400));
 log('쿠폰 등록 완료. 아래 쿠폰 전체를 해당 학생에게 전달하세요.');
});
bind('restore',async()=>{$('issued').value=localStorage.getItem('mythic-last-coupon')||'';log('마지막 초안을 불러왔습니다. 등록 상태 확인을 누르세요.');});
bind('copy',async()=>{await navigator.clipboard.writeText($('issued').value);log('쿠폰 전체를 복사했습니다.');});
async function couponState(text) {
 const p=parse(text), c=await collection();
 if(p.contract.toLowerCase()!==(await c.getAddress()).toLowerCase())throw Error('쿠폰과 위 계약 주소가 다릅니다.');
 const info=await c.coupons(keccak256(p.secret));return {p,c,info};
}
async function describe(text) {
 const {c,info}=await couponState(text);
 if(info.recipient===ZeroAddress){log('등록되지 않은 쿠폰입니다. 관리자 등록 승인을 확인하세요.');return;}
 let msg='수령 주소: '+info.recipient+'\n기한: '+new Date(Number(info.deadline)*1000).toLocaleString()+'\n취소: '+info.cancelled;
 if(info.tokenId>0n)msg+='\n발행 완료 tokenId: '+info.tokenId+'\n현재 소유자: '+await c.ownerOf(info.tokenId);
 else msg+='\n아직 발행되지 않았습니다.';
 log(msg);
}
bind('checkIssued',()=>describe($('issued').value));
bind('check',()=>describe($('coupon').value));
bind('redeem',async()=>{
 const {p,c,info}=await couponState($('coupon').value);
 if(info.tokenId>0n){await describe($('coupon').value);return;}
 if(p.recipient.toLowerCase()!==(await signer.getAddress()).toLowerCase())throw Error('쿠폰에 지정된 학생 지갑으로 전환하세요.');
 await wait(await c.redeem(p.secret));await describe($('coupon').value);
});
bind('cancel',async()=>{const p=parse($('issued').value),c=await collection(true);if(p.contract.toLowerCase()!==(await c.getAddress()).toLowerCase())throw Error('계약 불일치');await wait(await c.cancelCoupon(keccak256(p.secret)));log('쿠폰 취소 완료');});
bind('mint',async()=>{const c=await collection(true);await wait(await c.adminMint(address($('recipient').value.trim())));log('직접 발행 완료. 학생 지갑에서 목록 조회하세요.');});
bind('inventory',async()=>{
 const c=await collection(), who=await signer.getAddress(), count=await c.balanceOf(who), ids=[];
 for(let i=0n;i<count&&i<100n;i++)ids.push((await c.tokenOfOwnerByIndex(who,i)).toString());
 $('owned').textContent='보유 수: '+count+'\ntokenId (최대 100개 표시): '+ids.join(', ');log('조회 완료');
});
bind('transfer',async()=>{
 const c=await collection(), id=$('tokenId').value.trim(), to=address($('transferTo').value.trim()), from=await signer.getAddress();
 if(!/^[1-9][0-9]*$/.test(id))throw Error('tokenId는 양의 정수입니다.');
 if((await c.ownerOf(id)).toLowerCase()!==from.toLowerCase())throw Error('내가 소유한 NFT가 아닙니다.');
 await wait(await c['safeTransferFrom(address,address,uint256)'](from,to,id));log('NFT 전송 완료. 받는 지갑에서 목록 조회하세요.');
});
