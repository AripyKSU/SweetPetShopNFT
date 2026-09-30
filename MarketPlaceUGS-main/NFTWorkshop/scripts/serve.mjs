import http from 'node:http';
import fs from 'node:fs';
import path from 'node:path';
import {fileURLToPath} from 'node:url';
const root=path.resolve(path.dirname(fileURLToPath(import.meta.url)),'../web');
http.createServer((req,res)=>{
  const files={'/':'index.html','/app.mjs':'app.mjs','/ethers.min.js':'ethers.min.js','/contract.json':'contract.json'};
  const name=files[new URL(req.url,'http://localhost').pathname];
  if(new URL(req.url,'http://localhost').pathname==='/fennec-preview.png') {
    res.writeHead(200,{'Content-Type':'image/png','Cache-Control':'no-store','X-Content-Type-Options':'nosniff'});
    fs.createReadStream(path.resolve(root,'../../Assets/Art/PetShop/Pets/NFT/pet_fennec_nft.png')).on('error',()=>res.destroy()).pipe(res);
    return;
  }
  if(!name){res.writeHead(404);res.end();return;}
  const mime=name.endsWith('.html')?'text/html; charset=utf-8':name.endsWith('.json')?'application/json':'text/javascript';
  res.writeHead(200,{'Content-Type':mime,'Cache-Control':'no-store','X-Content-Type-Options':'nosniff'});
  fs.createReadStream(path.join(root,name)).pipe(res);
}).listen(8787,'127.0.0.1',()=>console.log('Open http://127.0.0.1:8787 in Chrome with MetaMask. Ctrl+C stops.'));
