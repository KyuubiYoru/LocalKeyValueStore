# LocalKeyValueStore

A [NeosModLoader](https://github.com/zkxs/NeosModLoader) mod for [Neos VR](https://neos.com/) that does a way to store data in a local db through logiX.

## Installation
1. Install [NeosModLoader](https://github.com/zkxs/NeosModLoader).
1. Place [LocalKeyValueStore.dll](https://github.com/GithubUsername/LocalKeyValueStore/releases/latest/download/LocalKeyValueStore.dll) and [CustomEntityFramework.dll](https://github.com/KyuubiYoru/CustomEntityFramework/releases/latest/download/CustomEntityFramework.dll) into your `nml_mods` folder. This folder should be at `C:\Program Files (x86)\Steam\steamapps\common\NeosVR\nml_mods` for a default install. You can create it if it's missing, or if you launch the game once with NeosModLoader installed it will create the folder for you.
1. Start the game. If you want to verify that the mod is working you can check your Neos logs.

### Usage
1. First, add a DynamicVariableSpace named "CF" to a Slot in NeosVR.
2. Next, add the string variables "Key" and optionally "Table" to the DynamicVariableSpace.
3. Finally, add any value type or ref Type Slot variable "Value" to the DynamicVariableSpace.

Once you've set up the DynamicVariableSpace, create a DynamicImpulseTriggerWithValue of type Slot and pass the Slot with the DynamicVariableSpace to it. The tag will determine the action that will be performed:

#### To write a value to the database with the specified key and table name, use the tag "cf.lkvs.write". 

#### To read a value from the database with the specified key and table name, use the tag "cf.lkvs.read".


Note that the table is optional. If you don't specify a table, the table name "default" will be used.

You can use the same key to store values of different types (e.g., strings, integers, booleans), but you cannot have the same key storing values of the same type in the same table.

If you read a value that doesn't exist, the DynamicVariable "Value" will not be set.

Tables and entries are automatically created when you write to them.

### Examples
[Include examples or screenshots here to help users understand how to use the mod.]

