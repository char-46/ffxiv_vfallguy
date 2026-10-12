# ffxiv_vfallguy dalamud plugin
vFallguy dalamud plugin aims to make the Fall Guys Collaboration event in FFXIV less of a hassle

## Fork 说明（char-46 修改版）

- 修复上游提交 `7715d9a` 引入的回归：`GameEvents.ProcessActionEffectPacketDetour` 误将 `header->ActionType` 作为 actionId 传出，导致第三关算路/策略/AoE 标记全部失效（上游 issue #15 / #16）
- `Map3.OnStartCast` 的候选序列补上 `_mech4RectsC`，消除中央列矩形每 6 秒刷 `Failed to find AOE` 错误
- UI 文本汉化（直接修改字符串，未引入 i18n 框架）

Dalamud repository:
```
https://raw.githubusercontent.com/char-46/ffxiv_vfallguy/master/repo.json
```

原版仓库：https://github.com/awgil/ffxiv_vfallguy

# How to install

1) Open the Dalamud Settings menu in-game
2) Under Custom Plugin Repositories, enter ```https://puni.sh/api/repository/veyn``` into the empty box at the bottom
3) Click the "+" button.
4) Click the "Save and Close" button.
5) You will now be able to find the plugin in your Plugin Installer and install it

# Current list of features
Automatically registers for the Fall Guys minigame 

Automatically leaves the instance if you are not the only person in it (Useful for finding solo instances)

Built-in button to leave the instance quickly (skips the confirmation button)

Adds markers to showcase upcoming AoEs on stage 3 of the minigame
