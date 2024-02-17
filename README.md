# Overview
EasySync aims to be the easiest way of syncing configuration between host and players (clients) for Lethal Company so that you can focus on creating the meat of your mod. All you'd have to do is to write your config class!  

Most config syncing guides (I've seen) require you to copy paste a bunch of code such as the request, receive, patches, etc... So why not reduce the amount of duplicate code and put it in one place? This will handle that for you.

# How To Use
I actually wrote a [wiki](https://thunderstore.io/c/lethal-company/p/Dreadrith/EasySync/wiki/). I didn't like that :(

### How does it work?
**TLDR: Reflection based code to handle the static members of CSync due to its generic type requirement.**

Cursed code that uses reflection to be able to handle CSync's requirement for generic types. Static members are not one and the same in generic types and one exists for each generic type. Using reflection, it grabs the members needed for CSync's functionality and handles them the same way as CSync requires so that there isn't the same code for each different class that inherits from CSync's SyncedInstance class.

## Credits
[Owen3H](https://github.com/Owen3H) - [CSync](https://github.com/Owen3H/CSync) - Made this possible.  
[icons8](https://icons8.com/) - Mod icon
