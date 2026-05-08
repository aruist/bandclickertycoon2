# Rally Tycoon Idle Clicker

## Game Design Document

## 1. Vision
**Rally Tycoon Idle Clicker** is an idle progression game where players build profitable race tracks, optimize production chains, and actively collect earnings from track vaults. The game combines idle growth, strategic upgrade investment, and periodic manual collection with ad-driven bonus opportunities.

The core experience is:
- Grow each race track's income through upgrades.
- Manage vault capacity so production does not stall.
- Use temporary multipliers and collection bonuses to accelerate progression.

## 2. Core Systems

### 2.1 World Structure
The game contains multiple race tracks, each represented by a `PlaceItem`.

Each `PlaceItem` contains:
- `improvements` (`List<ImprovementItem>`)
- `vault` capacity tiers (`kassaKaappiKoot`)
- current collected track money (stored per place and transferred to player on collection)

### 2.2 Improvement Economy
Each race track has multiple `ImprovementItem` entries that:
- produce profit over time
- can be upgraded repeatedly
- increase output as level grows

Upgrades are the primary long-term progression driver.

### 2.3 Vault Economy
Vault progression is represented by:
- capacity tiers: `kassaKaappiKoot` (defined on place)
- player vault level index per place: `playerData.places[].kassaKaappiKoko`

Design intent:
- track income is accumulated into that track’s vault
- if vault reaches capacity, the track stops generating additional income
- player must empty vault periodically to resume full generation

Collection flow:
1. place generates money into its vault
2. player taps to collect
3. collected money transfers into player `earnedMoney`

## 3. Runtime Architecture

### 3.1 Main Gameplay Manager
`paIkKaHaLlItSiJa`:
- owns and manages all `PlaceItem` instances (`placeDictionary` concept / place list ownership)
- handles place navigation and active track context
- routes money collection from place vaults to player money
- exposes place and vault data to UI

### 3.2 UI Manager
`KAyttOLiITTyma`:
- controls main HUD and dialogs
- manages transitions between gameplay and modal UI screens
- routes user actions to relevant systems

## 4. Main Interface

The primary gameplay interface is split into two regions:

- **Top section**: 3D race track view for current place
- **Bottom section**: improvement buttons for that place (purchase/upgrade actions)

Persistent HUD:
- **Top bar**: player’s current money

Middle action buttons:

1. **Multiplier Bonus Button**
   - Opens **Coin Multiplier** dialog
   - Contains roulette rewards: `2x`, `3x`, `4x` money bonus for limited duration
   - Spin is unlocked by watching an ad

2. **Vault Button**
   - Opens **Vault** dialog
   - Shows all places and current vault values
   - Contains:
     - **Collect**: transfer vault money to player
     - **Double**: double collection reward after ad watch

3. **Chest Button**
   - Opens **Chest** dialog
   - Current chest set:
     - `Free reward`
     - `Tap to unlock (3h)` x3
   - Rewards include diamonds and money
   - Detailed logic still to be finalized

4. **Shop Button**
   - Opens shop
   - IAP design is still in progress

5. **Settings Button**
   - Audio settings (SFX and music volume)
   - Credits

6. **Improvement Multiplier Button (`1x`, `10x`, `50x`)**
   - Changes bulk purchase amount
   - Allows rapid progression without repeated taps

## 5. Core Gameplay Loop

1. Select/enter a race track
2. Upgrade improvements to increase production
3. Let place generate money into vault
4. Collect vault to convert into player currency
5. Buy more upgrades and unlock stronger tracks
6. Use temporary multiplier systems and ad bonuses to speed progress

## 6. Current Design Gaps (To Finalize)

### 6.1 Chest System
Open points:
- exact unlock timer behavior
- reward tables and scaling by progression
- free chest cooldown
- guaranteed minimum rewards vs random drops

### 6.2 Shop / IAP
Open points:
- product catalog (currency, boosters, no-ads, bundles)
- progression impact and pay-to-win constraints
- integration with multiplier/vault loops

### 6.3 Vault Cap Enforcement
The design states that full vault halts production. Ensure this is consistently enforced in runtime economy rules and clearly communicated in UI.

## 7. Design Goals

- **Readable progression**: player always understands next best action.
- **Meaningful idle returns**: income grows while away, bounded by vault mechanics.
- **Active engagement moments**: collection, roulette, chest, and bulk upgrades create frequent decisions.
- **Fair acceleration**: ads and bonuses speed progress without replacing core strategy.

---
Version: Draft v1  
Project: Rally Tycoon Idle Clicker  
Location: `Doc/Rally_Tycoon_Idle_Clicker_GDD.md`
