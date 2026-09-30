# 별빛 페넥여우 NFT 배포

기존 `MythicSwordNFT`는 배포 시 정한 `tokenURI`를 변경할 수 없다. 새 `FennecPetNFT`는 별도 Sepolia 컬렉션이며 기존 NFT와 쿠폰은 자동 이전되지 않는다. Unity 내부 아이템 ID `MYTHIC_SWORD_NFT`와 쿠폰 형식 `MSW1`은 현재 Cloud Code 및 클라이언트와의 호환을 위해 그대로 사용한다.

1. `Assets/Art/PetShop/Pets/NFT/pet_fennec_nft.png`를 IPFS 또는 공개 HTTPS 저장소에 업로드한다. 지갑마다 접근할 수 있는 주소여야 한다.
2. 이 폴더에서 `npm run build`, `npm start`를 실행하고 PC Chrome에서 `http://127.0.0.1:8787`을 연다.
3. 공개 PNG 주소를 입력해 이미지 검사를 한다. `JSON 업로드 없이 메타데이터 만들기`를 누르면 작은 JSON이 `data:application/json;base64,...` URI로 자동 생성된다. 입력이 `ipfs://`이면 JSON의 `image`에는 공개 Pinata HTTPS 게이트웨이 주소를 담는다. JSON 파일 업로드는 필요 없다.
4. 메타데이터 검사를 한다. MetaMask에서 Sepolia와 관리자 지갑을 선택한 뒤 새 계약을 배포한다. 배포에는 Sepolia ETH 가스비와 MetaMask 승인이 필요하다. 별도 JSON 업로드 방식도 선택할 수 있다.
5. 새 계약 주소를 저장하고 Unity Scene 1의 `ReownWalletBridge.mythicNftContract`와 UGS Cloud Save `nft_config.contractAddress`에 **같은 새 주소**를 설정한다. 기존 주소를 쓰는 미수령 쿠폰은 새 계약에 재등록해야 한다. Cloud Code 코드는 주소 설정을 읽으므로 재배포가 필요하지 않다.
6. 테스트 지갑에 1개 발급한 후 사이트에서 tokenId를 입력하고 `발급 NFT 메타데이터 확인`을 누른다. 온체인 `tokenURI`와 공개 JSON `image`가 페넥여우 주소인지 확인한다. MetaMask의 Sepolia NFT 탭에서 같은 토큰을 확인한다. 자동 표시되지 않으면 계약 주소와 tokenId로 수동 가져오기한다.

배포 후 URL과 그림 내용은 임의로 바꾸지 말 것. 이 계약은 모든 토큰이 같은 메타데이터를 사용하며 `tokenURI` 변경 기능이 없다. 2026-09-29 배포 거래 `0x7cfce603a64305ca308598893d6585f89295b2722dabb8d2931ffe30c31ce176`의 새 계약 주소는 `0x51a756cdbb87fb37b7f796aebfb54a59306aefe6`이다. Unity Scene 1과 UGS 프로젝트 `220ad926-8415-4f2c-864b-877d256a005a`의 `production` / Cloud Save / `simple_market` / Default / `nft_config.contractAddress`에 같은 주소가 반영된 것을 2026-09-29 확인했다.

현재 위 계약의 tokenId 1은 메타데이터 JSON을 온체인에 내장하고 `image`를 `ipfs://bafybeian6ucwmadgccda35wsdlpfkftzb4t4yv7imxn4jctqg4cw767py4`로 지정한다. MetaMask 확장 프로그램에서 이름은 표시되지만 이미지가 표시되지 않는 사례가 확인됐다. 공개 PNG는 브라우저에서 정상이며 기존 검 NFT의 이미지가 `data:image/svg+xml;base64,...`로 내장된 것과 차이가 있다. 2026-09-29 이후 로컬 배포 사이트에서 생성하는 메타데이터는 공개 HTTPS 게이트웨이를 image로 사용한다. 이 변경은 이미 배포된 계약과 발급된 토큰에는 소급 적용되지 않는다.

2026-09-29 새 배포 계약 `0xb04cC32F512Bcaad59D3680FAf63072b58975Ec3`은 Sepolia에서 `Starlight Fennec Pet NFT` / `FENNEC`로 확인했다. Unity Scene 1과 UGS `production` / `simple_market` / Default / `nft_config.contractAddress`를 이 주소로 변경하고 저장값을 확인했다. 학생 지갑 `0x4dc9e2ae969a743695B8Cc4e344e0Ff22f813FeA` 대상 `CouponRegistered` 거래 `0x90e2549bc0d3678ccc11a33b9060e2c9db16526c0980d35ca68aac75541ea5c8`는 확인했으나, 당시 `nextTokenId=1`이고 `CouponRedeemed` 이벤트가 없어 NFT 수령은 아직 완료되지 않았다.

후속 확인: 새 계약의 tokenId 1은 학생 지갑 `0x4dc9e2ae969a743695B8Cc4e344e0Ff22f813FeA`가 소유한다. `tokenURI(1)`의 JSON `image`는 `https://gateway.pinata.cloud/ipfs/bafybeian6ucwmadgccda35wsdlpfkftzb4t4yv7imxn4jctqg4cw767py4`다. 사용자가 MetaMask에서 새 NFT가 표시되는 것을 확인했다. Unity 게임 인벤토리의 실제 동기화 결과는 별도 확인이 필요하다.
