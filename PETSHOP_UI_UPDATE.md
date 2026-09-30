# 펫샵 UI 개편 (2026-09-30)

## 적용 범위

- 운영 씬: `MarketPlaceUGS-main/Assets/Scene/1.unity`.
- **경매장 울타리와 인벤토리 러그 원본을 유지한다. 이 두 공간에 박스 UI를 덮지 않는다.** 환경 Image의 tint alpha는 1이며, 원본 PNG의 투명 영역은 유지한다. 펫 스프라이트의 투명 배경은 변경하지 않는다.
- 로그인, 우측 서비스 패널, 상단 도구 영역, 버튼과 거래 확인창에 원목/크림/세이지 UI를 적용했다. 기존 인증·결제·NFT·거래 이벤트와 참조를 보존했다.
- 테스트용 흰색 펫과 내부 테스트 문구를 로그인 안내로 교체했다. 중복 새로고침 버튼, 남아 있던 Coin 라벨, 인벤토리 가격표를 숨겼다.
- 한글 동적 TMP 폰트, 제한된 자동 글자 크기, 영역 밖 넘침 차단, 입력칸 내부 여백을 적용했다.

## 펫 공간 조작

- 280×280 셀의 한 줄 배치. 화면 너비를 초과할 때만 가로 스크롤이 켜진다. 16:9 기준 1920×1080과 1280×720에서는 **5마리부터** 넘길 수 있다.
- 빈 공간 드래그, 스크롤바, 마우스 휠을 사용할 수 있다.
- 펫에서 시작한 좌우 드래그는 스크롤, 위아래 드래그는 기존 거래 흐름이다. 클릭 후 기존 구매/판매/취소 버튼도 사용할 수 있다.
- 자동 갱신 중 스크롤 드래그도 펫 드래그 상태로 취급하여 진행 중인 조작을 보호한다.
- 항목이 줄거나 화면 크기가 바뀌면 스크롤 위치를 유효 범위 안으로 보정한다.

## 재적용 및 검증

- `Tools > Pet Shop > Apply Polished UI`: 1번 씬의 UI와 펫 프리팹에 스타일 재적용. 이후 수동 레이아웃 변경이 있으면 이 메뉴가 지정한 배치를 덮어쓰므로 주의한다.
- `Tools > Pet Shop > Validate and Capture UI`: 씬 Canvas 복제본에서 0/1/4/5/30마리, 두 해상도, 마지막 항목 도달, 가로 스크롤/세로 거래 드래그 분리를 검증한다. 샘플은 실제 인벤토리나 서버에 저장되지 않는다.
- 결과와 렌더: 프로젝트의 `Temp/PetShopUI/` (Unity가 종료 시 제거할 수 있음).
- 최종 검증 결과는 저장소 루트 `UI-Verification/`에 보존했다. 두 해상도에서 항목 수별 스크롤, 마지막 펫 도달, 가로/세로 드래그 분리 검증이 통과했다. 렌더에 등장하는 펫과 가격은 검증용 샘플이다.
- Unity Play 모드에서 로그인 전 안내, 우측 패널 표시, 빈 목록의 스크롤바 숨김을 확인한 뒤 편집 모드로 복귀했다. 최종 UI 컴파일 오류는 없었다.
- 온라인 두 계정 거래, 결제, NFT 발급 및 WebGL 실기 검증은 이 UI 검증에 포함하지 않는다.

## 이미지 제작 기록

기본 내장 image_gen 도구 사용. 최종 자산은 아래 프로젝트 경로에 저장했으며, 9-slice 설정은 적용 도구가 구성한다.

- `Assets/Art/PetShop/UI/panel_oak.png`
- `Assets/Art/PetShop/UI/button_sage.png`

제작 프롬프트:

1. 패널: "ONE production game UI panel sprite, square, straight-on orthographic. Warm cozy storybook pet shop, beige cream opaque blank interior with subtle paper surface, rounded honey oak wooden border, restrained carved corner detail, thin sage-green inner keyline. Empty center for readable UI and 9-slice stretching. Transparent exterior, opaque interior. No text, icons, pets, perspective or scene."
2. 버튼: "ONE standalone production Unity game UI button sprite. Wide horizontal rounded rectangle 3:1. Cozy storybook pet shop, hand-painted semi-flat sage green enamel inset, slim honey oak rim, subtle raised bevel, clean rounded corners. Plain opaque center for white Korean text. Edge details for 9-slice resizing. Transparent exterior. No text, symbols, icons, scene or perspective."
