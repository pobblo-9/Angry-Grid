# Unity Scene Setup Guide for Angry Grids Multiplayer

## Issue Analysis
From your debug messages, the problem is that your **NetworkObjects aren't spawning properly**. This is a common Unity Netcode setup issue.

## Required GameObject Setup

### 1. **TurnManager GameObject**
```
TurnManager (GameObject)
├── TurnManager.cs (script)
├── NetworkObject (component) ⚠️ CRITICAL
└── Set as prefab and add to NetworkManager prefab list
```

### 2. **SlingShotController GameObjects**
```
Player1Slingshot (GameObject)
├── SlingShotController.cs (script with playerNumber = 1)
├── NetworkObject (component) ⚠️ CRITICAL
├── Rigidbody (component)
├── Collider (component - BoxCollider/SphereCollider)
└── Renderer (component for visual)

Player2Slingshot (GameObject)  
├── SlingShotController.cs (script with playerNumber = 2)
├── NetworkObject (component) ⚠️ CRITICAL
├── Rigidbody (component)
├── Collider (component - BoxCollider/SphereCollider)
└── Renderer (component for visual)
```

### 3. **TicTacToeBoard GameObject**
```
TicTacToeBoard (GameObject)
├── TicTacToeBoard.cs (script)
├── NetworkObject (component) ⚠️ CRITICAL
└── 9 child TicTacToeSquare GameObjects (each with NetworkObject)
```

### 4. **TicTacToeSquare GameObjects**
```
TicTacToeSquare_0 through TicTacToeSquare_8 (GameObjects)
├── TicTacToeSquare.cs (script)
├── NetworkObject (component) ⚠️ CRITICAL
├── Collider (component)
└── Renderer (component)
```

## Step-by-Step Fix

### Step 1: Add NetworkObject Components
1. Select your **TurnManager** GameObject in the hierarchy
2. Click **Add Component** → **Netcode** → **NetworkObject**
3. Repeat for all SlingShotController, TicTacToeBoard, and TicTacToeSquare GameObjects

### Step 2: Create Prefabs
1. Drag each GameObject with a NetworkObject into your **Assets/Prefabs** folder
2. This creates prefab assets that can be referenced by NetworkManager

### Step 3: Configure NetworkManager Prefabs List
1. Select your **NetworkManager** GameObject
2. Find **Network Prefabs** list in the inspector
3. Click **+** to add each prefab:
   - TurnManager prefab
   - Player1Slingshot prefab  
   - Player2Slingshot prefab
   - TicTacToeBoard prefab
   - All 9 TicTacToeSquare prefabs

### Step 4: Set NetworkObject Spawn Behavior
For each NetworkObject component:
- **Don't Spawn With Observer**: Leave unchecked
- **Spawn With Observer**: Check this if you want all clients to see the object

### Step 5: Configure SlingShotController Settings
On your SlingShotController scripts:
- **Player1Slingshot**: Set `playerNumber = 1`
- **Player2Slingshot**: Set `playerNumber = 2`

## Alternative Quick Fix (If Above Doesn't Work)

If you're having trouble with the prefab setup, you can disable the NetworkObject spawning check temporarily:

### Method 1: Bypass Network Spawning Check
```csharp
// In SlingShotController.cs, temporarily modify CanHandleInput():
bool CanHandleInput()
{
    // TEMPORARY: Bypass network spawning check
    return isActive; // Just check if active, ignore network spawning
}
```

### Method 2: Scene-Based Network Objects
Instead of spawning objects dynamically, mark them to spawn automatically:
1. Select each NetworkObject in your scene
2. Check **"Auto-Spawn"** if available, or
3. Set the NetworkObject to spawn immediately when the network starts

## Testing Your Setup

### Expected Console Messages (After Fix):
```
TurnManager Instance set: TurnManager
Spawning network game objects...
Spawning TurnManager
Spawning SlingShotController for Player 1
Spawning SlingShotController for Player 2
Spawning TicTacToeBoard
TurnManager spawned - IsHost: True, ClientId: 0
OnMouseDown triggered for Player 1
OnMouseDown accepted for Player 1 - starting aiming  ✅
CanHandleInput check for Player 1: NetworkSpawned=true, Active=true  ✅
```

### If You Still Get "Spawned=false":
1. Check that **NetworkObject components** are added
2. Verify **NetworkManager prefab list** includes your objects
3. Make sure **StartGame()** is called and runs the spawning code
4. Check that the **Host** (not client) is starting the game

## Quick Verification Checklist:
- [ ] TurnManager has NetworkObject component
- [ ] Both SlingShotControllers have NetworkObject components  
- [ ] All prefabs are in NetworkManager's prefab list
- [ ] Game is started by the Host player
- [ ] Console shows "Spawning network game objects..." message

Try these fixes and let me know what console messages you get! The key is making sure all your game objects have **NetworkObject components** and are properly registered with the **NetworkManager**.