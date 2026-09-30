import {test} from 'node:test';
import assert from 'node:assert/strict';
import fs from 'node:fs';
import ganache from 'ganache';
import {BrowserProvider,ContractFactory,keccak256,randomBytes,hexlify} from 'ethers';
const rejectsTx = async promise => assert.rejects(async()=>{const tx=await promise; await tx.wait();});
const artifact=JSON.parse(fs.readFileSync(new URL('../web/contract.json',import.meta.url)));
test('NFT coupon lifecycle, authority, recipient, replay, expiry, cancellation, transfers and cap',async()=>{
 const chain=ganache.provider({logging:{quiet:true},chain:{chainId:11155111,hardfork:'shanghai'},wallet:{totalAccounts:4}});
 try{
 const p=new BrowserProvider(chain, undefined, {cacheTimeout: -1});p.pollingInterval=10;
 const admin=await p.getSigner(0), student=await p.getSigner(1), other=await p.getSigner(2);
 const a=await admin.getAddress(),s=await student.getAddress(),o=await other.getAddress();
 const c=await new ContractFactory(artifact.abi,artifact.bytecode,admin).deploy(a,'ipfs://example-metadata',3);await c.waitForDeployment();
 assert.equal(await c.name(),'Starlight Fennec Pet NFT');assert.equal(await c.symbol(),'FENNEC');
 const now=(await p.getBlock('latest')).timestamp;
 const secret=()=>hexlify(randomBytes(32));const key=secret();const hash=keccak256(key);
 await rejectsTx(c.connect(student).registerCoupon(hash,s,now+1000));
 await rejectsTx(c.connect(student).adminMint(s));
 await (await c.registerCoupon(hash,s,now+1000)).wait();
 await rejectsTx(c.registerCoupon(hash,o,now+1000));
 await rejectsTx(c.connect(other).redeem(key));
 await (await c.connect(student).redeem(key)).wait();
 assert.equal(await c.ownerOf(1),s);assert.equal(await c.balanceOf(s),1n);assert.equal(await c.tokenURI(1),'ipfs://example-metadata');
 await rejectsTx(c.connect(student).redeem(key));
 await (await c.connect(student)['safeTransferFrom(address,address,uint256)'](s,o,1)).wait();
 assert.equal(await c.balanceOf(s),0n);assert.equal(await c.tokenOfOwnerByIndex(o,0),1n);assert.equal(await c.ownerOf(1),o);
 const cancelled=secret();await(await c.registerCoupon(keccak256(cancelled),s,now+1000)).wait();await(await c.cancelCoupon(keccak256(cancelled))).wait();await rejectsTx(c.connect(student).redeem(cancelled));
 const exp=secret();await(await c.registerCoupon(keccak256(exp),s,now+100)).wait();await chain.request({method:'evm_increaseTime',params:[200]});await chain.request({method:'evm_mine',params:[]});await rejectsTx(c.connect(student).redeem(exp));
 await(await c.adminMint(s)).wait();await(await c.adminMint(s)).wait();await rejectsTx(c.adminMint(s));assert.equal(await c.totalSupply(),3n);
 const unregistered=secret();await rejectsTx(c.connect(student).redeem(unregistered));
 }finally{await chain.disconnect();}
});

