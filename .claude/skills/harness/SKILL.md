---
name: harness
description: Claude plans and reviews, Codex implements. Use for multi-step implementation work in this repository, or when the user says /harness, "하네스", "코덱스에게 시켜", "plan and delegate", "자동으로 끝까지", "작업 큐".
---

# 하네스: Claude가 계획하고 Codex가 구현합니다

당신은 오케스트레이터입니다. 작업마다 planner 서브에이전트가 계획을 쓰고, 사용자(또는 큐의 `auto_approve`)가 승인하고, 스크립트가 스텝마다 Codex를 실행해 인수 조건(AC)을 검사하며, 커밋 전에 reviewer 서브에이전트가 모든 스텝을 리뷰합니다. 당신은 루프를 돌리고 판정을 실행하며, 끝난 계획을 base 브랜치에 머지합니다. 스텝 구현, 계획 작성, diff 리뷰는 직접 하지 않습니다. 작업을 여러 개 연달아 처리해도 컨텍스트가 커지지 않도록 diff, AC 출력, `result.json`은 읽지 말고 서브에이전트에게 경로만 넘기세요.

스크립트: 저장소 루트에서 `uv run .claude/skills/harness/scripts/harness.py <subcommand>`. 모든 서브커맨드는 JSON 객체 하나를 출력합니다. 종료 코드 2는 전제 조건이 실패해 아무 일도 일어나지 않았다는 뜻이니 `error`를 읽으세요.

커밋하는 서브커맨드(`start`, `commit`, `replan`, `finish`)에는 시스템 리마인더가 알려주는 git 커밋 attribution 줄을 한 줄씩 `--trailer`로 넘깁니다(예: `--trailer "Co-Authored-By: ..." --trailer "Claude-Session: ..."`). 스크립트는 모델 이름과 세션을 모릅니다. `Harness-Plan`과 Codex 트레일러는 스크립트가 직접 붙이니 넘기지 마세요.

## 서브에이전트

둘 다 `general-purpose`입니다. 프롬프트에는 읽을 가이드와 이번 호출에 필요한 값만 넣습니다. 규칙과 반환 형식은 가이드에 있습니다. 끝나면 반환 메시지만 읽고, 형식이 깨졌으면 같은 에이전트에게 SendMessage로 형식대로 다시 달라고 하세요.

- **planner**: `.claude/skills/harness/plan-guide.md`를 읽고 따르라고 합니다. 넘길 것: 요청 종류(새 계획 작성 / 기존 plan.yaml 수정 / 작업 분해), 작업 본문이나 목표, plan id. 수정이면 이유와 커밋된 스텝 id도 넘깁니다.
- **reviewer**: `.claude/skills/harness/review-guide.md`를 읽고 따르라고 합니다. 넘길 것: `result_file`, plan id, step id.

## 모드

사용자의 말에서 모드를 고릅니다.

- **plan** (`/harness plan <작업>`): 작업 하나를 "작업 처리 절차"로 머지까지 처리합니다. 승인은 항상 사용자가 합니다.
- **auto** (`/harness auto [목표]`): 작업 큐를 끝까지 처리합니다.
- **resume** / **status** (`/harness run`도 같음): 새 세션이 진행 중인 계획이나 큐를 이어받습니다.

## 작업 큐

`.harness/queue.yaml`(git에 추적되지 않음):

```yaml
auto_approve: false   # true면 계획을 사용자 확인 없이 승인
tasks:
  - task: |
      한 문단: 무엇을 만들고 무엇이 "완료"인지.
    plan: add-user-export   # 오케스트레이터가 planner를 띄우기 전에 채움
```

- 진행 상태는 `queue`로 봅니다. 작업마다 `new`(계획 없음), `drafted`(plan.yaml만 있음), `started`, `finished`(머지됨) 중 하나이고, `next`가 처리할 작업입니다(본문 포함). 완료 표시는 따로 하지 않습니다. 계획 파일에서 계산됩니다.
- `auto_approve`는 사용자가 파일에 썼거나 대화에서 명시적으로 켜라고 했을 때만 true로 바꿉니다.
- 사용자가 계획을 버리라고 하면 그 `.harness/plans/<plan-id>/` 디렉토리를 지우고 큐의 `plan`을 비웁니다. 작업을 빼라고 하면 그 항목도 지웁니다. 남은 draft는 다음 `start`를 막습니다.

### 모드: auto

1. 목표가 주어졌으면 planner에게 작업 분해를 맡깁니다. 돌아온 작업 목록과 현재 `auto_approve` 값을 사용자에게 보여주고 **멈춥니다**. 사용자가 목록을 승인하면 진행합니다. 목표 없이 시작했으면 바로 2단계로 갑니다.
2. `queue`를 실행합니다. `next`가 null이면 "마무리"로 갑니다.
3. `next.state`에 따라 진행합니다.
   - `new`: "작업 처리 절차" 1단계부터.
   - `drafted`: 이전 세션이 승인 전에 멈춘 계획입니다. planner에게 기존 plan.yaml 수정(점검과 요약)을 맡기고 2단계(승인)부터.
   - `started`: `status --plan <plan-id>`의 `current_branch`가 그 계획의 `branch`면 "스텝 루프"부터. 다르면 멈추고 보고합니다.
4. 작업이 머지되면 2단계로 돌아갑니다. 작업 사이에 멈춰서 묻지 마세요.

### 작업 처리 절차

1. **계획.** plan id를 정합니다: 소문자, 숫자, `-`(예: `add-user-export`)로 쓰고 `.harness/plans/`에 아직 없는 이름이어야 합니다. 큐 작업이면 planner를 띄우기 **전에** 큐 항목의 `plan`에 적습니다. 그다음 planner에게 새 계획 작성을 맡깁니다.
2. **승인.** planner가 돌려준 `questions`가 "없음"이 아니면 `auto_approve`여도 사용자에게 묻고 멈춥니다.
   - 사용자 승인(plan 모드이거나 `auto_approve: false`): 반환 내용을 간결하게 보여줍니다. 목표, 브랜치, 스텝마다 한 줄, 위험을 보여주고 YAML 전체는 붙여넣지 마세요. 수정 요청은 SendMessage로 그 planner에게 넘기고(세션이 바뀌어 없으면 새 planner에게 기존 plan.yaml 수정으로) 바뀐 내용을 다시 보여줍니다. 대화에서 명시적인 승인 없이 승인하지 마세요.
   - `auto_approve: true`: planner의 자체 점검을 믿고 바로 승인합니다.

   승인은 plan.yaml의 `approved: false`를 `true`로 바꾸는 것입니다. 파일 앞부분만 읽으면 됩니다.
3. **start.** `start <plan-id>`를 실행합니다. 계획 검증으로 거부되면 planner에게 `error`를 넘겨 기존 plan.yaml 수정을 맡기고 2단계로 돌아갑니다.
4. **스텝 루프** (아래).
5. **finish.** `complete: true`가 나오면 `finish`를 실행합니다. 계획 브랜치를 base 브랜치(`start` 때 있던 브랜치)에 `--no-ff`로 머지하고 계획 브랜치를 지웁니다. 사용자에게 한 줄로 알립니다: `작업 2/4 머지됨 (main abc1234): <plan-id>`. 충돌로 거부되면 멈춥니다.

### 스텝 루프

`complete: true`가 나오거나 멈춰야 할 때까지 반복합니다. `status`는 루프에 처음 들어갈 때만 실행합니다. `commit`과 `discard`도 `next_step`, `complete`, `dirty_files`, `done`/`total`을 돌려주므로 그 뒤에는 `status`를 다시 부르지 마세요.

1. `next_step`을 확인합니다. 루프에 처음 들어갈 때는 이전 세션이 남긴 상태를 `status`의 그 스텝(`status`, `attempts`, `last_result`)으로 판단합니다.
   - dirty 파일이 계획의 `plan.yaml` 하나뿐이면 계획 수정이 진행 중이던 것입니다. discard하지 말고 "실행 중 계획 변경"을 이어가세요.
   - 다른 dirty 파일이 있고 스텝이 `pending_review`면 판정을 받지 못한 시도입니다. `last_result`를 `result_file`로 3단계부터.
   - 다른 dirty 파일이 있고 스텝이 `pending`이면 중단된 실행이거나 discard 전에 끊긴 실패입니다. 시도를 다 썼으면(`attempts`가 plan.yaml의 `max_attempts`, 기본 3) 멈춘 상태 그대로이니 사용자에게 보고하고, 아니면 `discard <step-id>`.
   - tree가 깨끗하고 `attempts`가 0보다 크며 `.harness/plans/<plan-id>/runs/feedback-<step-id>-<attempts>.md`가 있으면 재시도 직전에 끊긴 것입니다. 그 파일을 `--feedback-file`로 넘겨 2단계로.
2. 스텝을 **백그라운드에서** 실행합니다(Codex는 최대 30분까지 걸릴 수 있어 포그라운드 Bash 호출 한도를 넘습니다): `uv run .claude/skills/harness/scripts/harness.py run <step-id> --brief`를 `run_in_background`로 실행하고 완료 알림을 기다립니다. 출력은 `status`, `attempt`, `max_attempts`, `result_file`뿐이고 전체 결과는 `result_file`에 있습니다.
3. **리뷰.** `status`와 상관없이 reviewer에게 `result_file`, plan id, step id를 넘기고 판정을 기다립니다.
   - `commit`: `commit <step-id>`. 판정 뒤 tree가 바뀌었다며 거부되면 멈추고 보고합니다.
   - `retry`: `attempt`가 `max_attempts`보다 작으면 `discard <step-id>` 뒤 `run <step-id> --brief --feedback-file <feedback_file>`로 2단계를 반복합니다. 시도를 다 썼으면 discard하지 말고 멈춥니다. 사용자가 그 tree를 직접 볼 수 있게 남겨 둡니다. 사용자가 요청하지 않는 한 `--force`를 넘기지 마세요.
   - `stop`: 멈추고 reviewer의 `reason`을 보고합니다.

   `git_state_changed`였다면 `discard`가 실행 직전 커밋으로 되돌리고 `removed_commits`를 보고합니다. 현재 브랜치가 바뀌어 `discard`가 거부되면 멈추고 보고하세요. reviewer의 `notes`는 "마무리" 요약에 쓰도록 기억해 둡니다.
4. `commit`이나 `discard` 뒤마다 사용자에게 한 줄로 알립니다: `스텝 2/5 커밋됨 (abc1234): <요약>` 또는 `스텝 2/5 시도 1 실패: <reason>, 재시도합니다`. 스텝 사이에 멈춰서 묻지 마세요.

### 멈춤 조건

다음 경우 멈추고 사용자에게 보고합니다. 큐를 처리하던 중이면 큐 전체가 멈추고, 사용자가 해결한 뒤 `/harness auto`로 이어갑니다.

- 한 스텝의 시도 횟수를 다 썼을 때
- reviewer가 `stop`을 냈을 때: Codex가 사람의 결정이 필요한 blocker를 보고했거나, 계획 자체가 틀렸거나(스텝이 불가능하거나 설계가 코드와 맞지 않음), 하네스가 만든 것으로 볼 수 없는 변경이 working tree에 있을 때
- planner가 `questions`를 돌려줬을 때, 작업 분해 결과가 승인을 기다릴 때
- `finish`가 거부됐을 때(base 브랜치와 충돌 등)

보고 내용: 어느 작업의 어느 스텝에서 무슨 일이 있었고, 무엇을 권하는지(계획을 고쳐 재시도, 스텝 제외, 사용자가 넘겨받기). 계획 변경은 다음 절을 따릅니다.

### 실행 중 계획 변경

`start` 이후 `plan.yaml`의 모든 변경은 처음 계획처럼 사용자의 명시적 승인이 필요합니다. `auto_approve`여도 마찬가지입니다.

1. 현재 스텝을 먼저 끝냅니다: `commit` 또는 `discard`. `plan.yaml` 외에 다른 변경이 있으면 `replan`이 거부합니다.
2. planner에게 기존 plan.yaml 수정을 맡깁니다. 바꿀 이유와 커밋된 스텝 id를 넘기세요. planner는 `approved: false`로 고쳐 둡니다.
3. 사용자에게 planner가 돌려준 `changes`를 보여줍니다: 추가·삭제·순서 변경된 스텝, 수정된 모든 `description`과 `ac`. AC가 약해졌다면 분명히 말하세요.
4. 명시적 승인을 받으면 `approved: true`로 바꾸고 `replan`을 실행합니다. `plan.yaml`만 따로 커밋하고 `added_steps`, `removed_steps`, `changed_steps`를 보고합니다.

스크립트가 강제하는 규칙: 커밋된 스텝은 순서와 내용 그대로 맨 앞에 있어야 합니다(커밋된 작업을 고치려면 새 스텝 추가). `max_attempts`와 `branch_prefix`는 바꿀 수 없습니다. 고친 스텝은 이미 쓴 시도 횟수를 유지합니다(다 쓰면 `--force` 여부는 사용자가 정합니다).

### 마무리

plan 모드는 `finish` 뒤에, auto 모드는 `queue`의 `next`가 null이 되면 한 번 요약합니다: 머지한 작업과 merge 커밋, reviewer `notes`에 모인 받아들인 범위 밖 파일과 미룬 것. push는 사용자가 직접 합니다.

### 모드: resume / status

`status`를 실행합니다. 계획이 어디까지 왔는지 짧은 목록으로 보고하고, 사용자가 계속하길 원하면 스텝 루프 1단계부터 이어가 `finish`까지 진행합니다. 진행 중인 계획이 없다는 에러가 나오면 `queue`로 큐 상태를 보고하고, 계속하길 원하면 auto 모드로 이어갑니다.

## 이 스킬에서 절대 하지 않는 것

- 스텝 코드를 직접 구현하거나 고치기. planner에게 계획을, reviewer에게 피드백을 맡기세요.
- 명시적 승인 없이 `approved: true`로 바꾸기. `auto_approve: true` 큐의 첫 승인만 예외이고, replan은 예외가 아닙니다.
- 사용자 요청 없이 `auto_approve` 켜기.
- `plan.yaml`을 직접 커밋하기. 계획 변경은 `replan`으로만 합니다.
- reviewer의 `commit` 판정 없이 `commit` 실행하기.
- `finish`가 아닌 방법으로 머지하기. rebase, push, 브랜치 직접 전환.
