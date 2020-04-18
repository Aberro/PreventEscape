# PreventEscape
Mount&amp;Blade 2: Bannerlord module which allows modifying heroes escape chances.

### DESCRIPTION:
This mod overrides default escape mechanic, significantly reducing nobles chance to escape. Also, prisoners barter and prices has been completely reworked.
### LIST OF FEATURES:
* Base escape chance is 10 times lower (1% per day) if captor is noble and usual (10% per day) if captor is bandits. If prisoner and captor factions are at war, escape chance is another 10 times lower (0.1% per day).  If prisoner is in player's party or settlement, escape chance is lower by third (0.33% per day if factions not at war and 0.033% if they are).
* Escape chance is (usually) higher if prisoner is kept in mobile party and depends on party size. In original game, party size was capped by 81 soldiers, in this mod party size modifier is left uncapped, so the larger your party is, the lower the chance of prisoners to escape, to the point when escape chance is lower than in settlement (though, your party size need to be over 800)
* Prisoners could pay for their ransom themselves if they have enough money, or they could be ransomed by their clan leaders or their faction leaders, the choice is random.
* Prisoners could be ransomed directly from captors, or from their clan leaders or their faction leader, the choice is random (you could suggest better mechanics for that and I will consider it).
* Prisoner price now depends on multiple factors: clan strength (50 gold per unit), clan renown (10 gold per unit), recruitment cost (depends on character level), relation of ransomer to prisoner, whether captor and prisoner factions is at war (price is doubled), whether prisoner is a king (price is quadrupled) or a clan leader (price is doubled), whether captor is bandits (price is 1/10 and does not increased by "At War" modifier) and how long prisoner has been kept.
* Captors reduce the price they want over time, by default it's halved each 5 days. Ransomer increase the price they will agree to pay, by default it's doubled each 5 days. Initially, captors will ask 4 times as much as ransomer would agree to pay, so they could agree on the price after 5 days.
* If player is clan or faction leader, he can take or release prisoners kept by his vassals.
* If prisoner is kept by player or his vassal, a ransomer (the prisoner himself, or his clan leader, or his faction leader) will contact player occasionally (10% chance per day per prisoner) and player can barter for prisoner release.
* If player is a vassal, a ransomer could barter with his lord, therefore player would have to release prisoner. If prisoner is kept by vassal of player, a ransomer could barter with that vassal.
* If player decline to talk with prisoner or ransomer, it will decrease their relations to player (by default, by 2). If player decline barter offer, it will decrease ransomer relation to player (by default, by 1). If player will agree to release prisoner without barter when ransomer asked, it will increase relations of both prisoner and ransomer by 5. If player will agree to release prisoner without barter when prisoner asked, it will increase relations with prisoner by 10.
* If prisoner was ransomed by other lord (both AI and player), it will increase his relation to ransomer by 10 and prisoner's clan leader and faction leader to ransomer by 5.
### THANKS TO:
SkarlathAmon for text cleanup
### KNOWN ISSUES:
### PLANNED FEATURES:
* Dialogue and barter with prisoners (planned in next major update, version 1.3)
* Additional conditions of prisoner release, i.e. forbid to attack player for set amount of time, lord defection (though, not to player's faction but as independent clan, but that's becomes possible after release), maybe other.
* Quest for ransoming lords as part of lord's defection.
* Ability to barter for player's clan members or vassals.
* AI strategy to free their faction' prisoners by force.
