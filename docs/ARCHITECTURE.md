# 아키텍처

> 선택 사항. 스텝 description의 "읽어야 할 파일"로 지정되는 문서입니다. 실제 코드와 어긋나면 코드가 아니라 이 문서를 고치세요.

## 디렉토리 구조
```
Codrio.slnx
global.json                    # SDK 10.0.x 고정
Directory.Build.props          # 공통 빌드 속성, 기본 버전(0.1.0), 패키지 메타데이터
LICENSE                        # MIT
.github/workflows/ci.yml       # Windows 러너에서 빌드·테스트
.github/workflows/release.yml  # v* 태그 push 시 태그 버전으로 nuget.org 배포
src/Codrio/                    # netstandard2.0, 런타임 의존성 없음
├── CSharp.cs                  # 진입점 정적 클래스
├── Builders/                  # 공개 빌더. 상태와 렌더링(internal Write)을 함께 가진다
├── Rendering/                 # CodeWriter, 식별자 검증·이스케이프, Type → C# 이름 변환, XML 이스케이프 (internal)
├── Polyfills/                 # netstandard2.0에 없는 컴파일러 지원 타입 (internal)
├── PublicAPI.Shipped.txt
└── PublicAPI.Unshipped.txt
tests/Codrio.Tests/            # xUnit v3, net10.0;net48, Microsoft.CodeAnalysis.CSharp 참조
```

## 레이어 규칙
- 빌더가 곧 모델이다. 빌더는 상태를 internal 필드로 들고 `internal void Write(CodeWriter)`로 자기 자신을 렌더링한다. 빌더와 1:1로 대응하는 모델 노드 계층을 따로 만들지 않는다. 이유: 같은 구조를 두 번 정의하지 않는다.
- `ToString()`은 새 `CodeWriter`를 만들어 `Write`를 호출한 결과다. 모든 빌더가 같은 방식이다. 예외: 한 줄 안에 들어가는 조각만 만드는 빌더(예: `TypeParameterBuilder`)는 `CodeWriter` 없이 조각 문자열을 만드는 internal 메서드를 두고, `ToString()`은 그 문자열을 줄바꿈 없이 돌려준다. 담는 빌더가 그 문자열을 자기 줄에 넣는다.
- 들여쓰기, 줄바꿈, 블록 열고 닫기는 `CodeWriter`만 한다. 빌더는 공백이나 `\n`을 직접 이어 붙이지 않는다.
- 공통 기능(접근 제한자, 수식어, attribute, 문서, 제네릭 매개변수)은 `TypeBuilder<TSelf>` 같은 제네릭 기반 클래스에 두고 `TSelf`를 반환해서 체이닝 타입을 유지한다. 멤버 빌더도 같은 방식이다.
- `src/Codrio`는 `Microsoft.CodeAnalysis`를 참조하지 않는다. Roslyn은 테스트 프로젝트에서만 쓴다.
- 식별자 검증과 키워드 이스케이프는 `Rendering/`의 헬퍼 한 곳에서 하고, 빌더는 이름을 받는 시점에 그 헬퍼를 호출한다.

## 데이터 흐름
```
사용자 호출 (CSharp.File/Class/... + 람다 또는 Add)
  → 빌더 상태 누적 (식별자 검증·@ 이스케이프는 이 시점)
  → ToString()
  → Write(CodeWriter) 재귀: File → Namespace → Type → Member → Body
  → 문자열
```

## 횡단 관심사
- 에러 처리: 잘못된 식별자는 받는 즉시 `ArgumentException`, null 인자는 `ArgumentNullException`을 던진다. 렌더링 단계에서는 예외를 던지지 않는다. 수식어 조합과 C# 언어 버전은 검증하지 않는다.
- 로깅/관측: 없음.
- 설정: 없음. 포맷 규칙은 `CodeWriter`에 고정되어 있다.
- 스레드 안전성: 없음. 빌더는 가변이다.
- 테스트: 출력을 기대 문자열과 정확히 비교한다. 완결된 파일 단위 출력은 `Microsoft.CodeAnalysis.CSharp`로 컴파일해 오류 진단이 0개인지 확인한다.
