# Up Iz Up — Current Full Plot

Supplied by the user on 2026-08-15. Authoritative story reference for both
the Mini and the larger game. Mission 1 as described here ("A Start in
Highland") is the design target `MINI-001`/`MINI-011` have been building
toward; later missions define NPC roles and systems to build in future
tasks (see `TASKS.md` and `PROJECT-HANDOFF.md` for what's actually
implemented versus planned).

## Main premise

Smart and Strong are two young men from Grand Bay, Dominica. They leave
school believing that working for themselves will give them a better
future than remaining in the classroom without money or opportunity.

They begin with honest farming in Highland—planting tomatoes, bananas, and
carrots and selling produce around Lalay. Farming teaches them patience,
crop quality, negotiation, transportation, and how the local market
operates.

The problem is that legal crops pay slowly.

When a fictional middleman offers them far more money to grow weed, they
enter a world involving exploitative employers, rival sellers, police
heat, territorial disputes, vehicles, hired crew members, and increasingly
dangerous opportunities in Roseau and Guadeloupe.

The central story is about whether Smart and Strong can build something of
their own without becoming like the people exploiting them.

Smart and Strong are both at least 18 years old. "The two boys" is how
they are known locally.

## The two protagonists

### Smart

Smart is the planner and negotiator.

He understands people, crop quality, prices, and business opportunities.
He farms faster, negotiates better legal-market prices, and is more
likely to notice when an employer is deceiving them.

Smart initially wants to build a legitimate farming business, but he
gradually becomes fascinated by the money and influence available through
the weed market.

His weakness is believing he can control every situation through
planning.

### Strong

Strong is physically tougher, more loyal, and capable of carrying more
produce. He has greater health and stamina and is more effective during
escapes, fights, and demanding deliveries.

Strong is initially more willing to take fast-money opportunities because
he wants immediate results. However, he becomes increasingly uncomfortable
when innocent people or their own community are endangered.

His weakness is reacting emotionally when someone betrays or disrespects
them.

The player can switch between Smart and Strong. They share money,
inventory, heat, land, vehicles, and mission progress, but retain separate
health, stamina, position, and abilities.

## Chapter One — A Start in Grand Bay

### Mission 1: A Start in Highland

Smart and Strong begin at a small family safehouse near Lalay.

A fictional farming mentor offers them tomato seedlings and permission to
use a neglected plot in Highland. The player learns how to switch between
the boys before walking through Lalay and following a rough dirt trail
into the farming area.

They plant tomatoes, water the soil, wait through visible growth stages,
and harvest their first crop.

The mission ends when they return to Lalay and sell the tomatoes to a
market woman.

The profit is disappointing.

Smart says they must improve quality and find better buyers. Strong points
out that they worked hard for very little money.

This establishes the story's main conflict: patience versus fast money.

### Mission 2: Lalay Delivery

A shopkeeper orders fresh produce but requires delivery before closing.

The player loads crates and travels by foot or borrowed bike. Smart can
negotiate a slightly better price, while Strong can carry more produce
without slowing down.

The mission introduces:

- Selling and inventory.
- Stamina and carrying capacity.
- Local reputation.
- Market prices.
- Bikes and transportation.
- Standing and walking residents.
- Friendly police presence around the Lalay road.

The boys earn their first meaningful local reputation.

### Mission 3: Market Pressure

Another fictional farming crew begins undercutting their prices and
warning vendors not to buy from them.

The player chooses how to respond:

- Lower prices temporarily.
- Improve crop quality.
- Complete a difficult timed delivery.
- Confront the rival crew.

The choice affects money, reputation, and future rivalry.

Smart realizes that farming is not only about growing food—it is also
about controlling supply, relationships, and market access.

### Mission 4: More Land

The original Highland plot is too small.

Smart and Strong earn enough money to rent or purchase another farming
lot. This introduces the future land system: suitable remote areas can be
purchased and converted into farming zones.

They also repair or gain access to a simple farm safehouse.

Legal crops expand to tomatoes, bananas, and carrots. Mature crops look
visibly different: red tomatoes, banana bunches, and harvest-ready
carrots.

## Chapter Two — Fast Money

### Current playable bridge into Fast Money

After the first legal deliveries, Sacat and Franki explicitly choose how quickly to approach Boss J. Pressing K goes directly to him. Pressing L continues two more legal tomato harvest-and-sale loops: the low pay becomes increasingly frustrating, Franki says, “Gasah, that frustrating me,” and Sacat reluctantly agrees to check the bossman despite hearing that he “does bobol people on paying.” Both choices therefore rejoin the same Boss J route without pretending that the temporary legal choice is a separate ending.

After the first Bushers handoff, the playable heat lesson is split into two clear beats: rest at the Highland safehouse, then approach two different regular police officers while carrying no weed or weed seeds. Normy is introduced separately as a corrupt contact; $100 removes 20% heat after his cooldown. Boss J's withheld or reduced payments are revealed in dialogue after delivery, not spoiled in the objective text.

### Approved revised Grand Bay progression after the early heat lesson

The user's 2026-08-21 playtest establishes this order for the next implementation packets. It supersedes the current late-mission ordering wherever the two conflict, while retaining existing save-compatible IDs.

1. **Clean Face repair:** Normy takes the mission payment once and the mission completes. His repeat heat service remains an ambient later interaction, not a reason to trap the mission.
2. **Normy's favour:** before sending the boys to the Boat Man, Normy asks for specific food and pharmacy items. After delivery, he admits he does not know who is taking their crop/stuff and suggests asking the Boat Man about Gardey Zafeh in Guadeloupe.
3. **Boat Man introduction:** he recognizes their hustle and offers future euro work, then agrees to connect them to the scene around Gardey in Gwada. Earlier dialogue should mention rising gang attention without revealing Dog Life too early.
4. **Rasta's strain school:** Rasta no longer requests tomatoes. He tests three Bushers, then three Black Sugar, three Purple, Blue Cheese and, later, the mixed strains Purple Sugar, Sugar Cheese and Purple Cheese. Each step is more valuable and remains hidden until its mission unlock.
5. **Grand Bay market confidence:** only after the boys master the strain ladder and build meaningful stock do they believe they are winning Grand Bay and become ready for Guadeloupe.
6. **Dog Life jealousy:** the rival gang reacts to the boys' growing stock and reputation. The boys must build their own crew before the block confrontation. Dog Life can regroup and return rather than being permanently erased.
7. **Guadeloupe before Roseau:** the inactive protagonist can travel with the Boat Man, disappear from the playable scene, remain unswitchable and return after a visible timer. Guadeloupe progression must be established before Roseau opens.
8. **Community counterweight:** Brakes, the priest, gives community/supply errands and health-restoring blessings without selling personal favors. This supports the legitimate/community side of the story while risk increases.

The cellphone becomes a progression reward: first to call the farming partner, later to call recruited crew for backup. These systems should be introduced through missions and hints rather than being available without context.

After "Round the Village" (M12), "Get Your Tool" leads Sacat and Franki to the Lalay black-market trader. Here "tool" means a gun. They buy the fictional Lalay Tool and fire one test shot away from people before the Rasta chapter continues. The trader and shop remain available afterward for ammunition and clothing resale.

### Mission 5: The Offer

A fictional middleman known as Boss J approaches them. The legacy internal identifier `BossK` remains save-compatible only and is never player-facing.

Boss K has been watching their deliveries and knows they can grow reliable
crops without attracting attention. He offers them Bushers, the cheapest
weed strain.

Before accepting, the game clearly shows:

- Much higher potential profit.
- Increased police heat.
- Rival territory risk.
- Damage to certain community relationships.

Smart is cautious. Strong argues that one harvest could make more than
several legal deliveries.

The player can initially refuse, but economic pressure eventually brings
the offer back.

### Mission 6: First Bushers Harvest

The boys plant Bushers in a remote Highland location.

Unlike legal crops, weed must be kept away from roads and heavily
populated areas. The crop grows through visible stages, and its quality
depends on watering and care.

They make their first illegal sale in Grand Bay.

The payment is substantially higher, but the police heat meter rises. A
police officer who previously spoke casually to them now reacts with
suspicion.

The boys understand that their behaviour is changing how Grand Bay sees
them.

### Mission 7: Territory Warning

A fictional Grand Bay rival crew claims the boys sold in an area
controlled by them.

The encounter can be resolved through:

- Dialogue.
- Paying a temporary fee.
- Escaping.
- A simple fight.
- Calling a recruited ally, if available.

Strong wants to confront them. Smart believes someone deliberately failed
to warn them about the territory.

They discover that Boss K sent them there knowing it would create
conflict. He is testing whether they can survive pressure while keeping
his own name out of the situation.

This is the first clear example of exploitation.

### Mission 8: Wheels

Boss K lends them a motorcycle or vehicle for a larger delivery.

The mission introduces entering, exiting, driving, vehicle damage, and
police attention. Bikes are especially useful on the narrow roads and
rough Highland routes.

After completing the delivery, Boss K deducts fuel, vehicle use, and an
invented "protection fee" from their payment.

Smart realizes the boys are earning more but remaining dependent.

## Chapter Three — Taking Grand Bay

### Mission 9: Black Sugar

The boys unlock Black Sugar, a higher-quality strain with better profit
and greater heat.

They must improve their farm, acquire better land, and protect the crop
from theft or damage.

Smart begins building direct relationships with buyers so they no longer
depend entirely on Boss K. Strong recruits one or two trusted fictional
Grand Bay characters who can guard crops, make local deliveries, or
accompany them.

This introduces crew recruitment and loyalty.

### Mission 10: Choosing the Market

Boss K orders them to stop selling independently.

The player chooses between:

- Continuing to work under Boss K.
- Secretly keeping part of each harvest.
- Publicly creating their own operation.
- Strengthening the legal farming business as cover and alternative
  income.

The decision changes Boss K's attitude, market prices, and rival
encounters.

### Mission 11: Purple

Purple becomes the best Grand Bay strain.

It produces the highest local profit but creates considerably more heat.
Rival crews attempt to steal a harvest or intimidate the boys' buyer.

The player must use both protagonists:

- Smart handles negotiations, crop planning, and market arrangements.
- Strong handles transportation, defence, and high-capacity deliveries.

Character switching becomes important rather than cosmetic.

### Mission 12: Grand Bay Market

Smart and Strong attempt to dominate enough of the Grand Bay market to
become independent.

Market domination does not mean controlling every person. It means
establishing:

- Reliable crops.
- Several buyers.
- Purchased or rented farmland.
- A working vehicle.
- Loyal crew members.
- Enough reputation that Boss K cannot easily remove them.
- A legal produce operation that still matters to the community.

Boss K retaliates by sending a rival crew and passing information to the
police.

The chapter ends with the boys surviving the attack or police pressure and
breaking Boss K's control over their Grand Bay operation.

Roseau becomes available.

## Chapter Four — Roseau Heat

Roseau offers considerably better prices, particularly for Black Sugar and
Purple, but it is more dangerous.

The city has:

- Larger markets.
- More police.
- Fictional territorial crews.
- Market vendors demanding payment.
- More traffic and pedestrians.
- More expensive safehouses.
- Buyers who can purchase larger quantities.
- Employers higher in the criminal chain.

### Mission 13: First Roseau Sale

The boys transport a limited shipment into Roseau.

A market vendor demands a fee, a territorial crew questions them, and
police patrols are more frequent. The player must decide whether to pay,
negotiate, relocate the sale, or risk confrontation.

The Roseau sale earns far more than Grand Bay, proving why people take the
risk.

### Mission 14: Different Employers

Smart and Strong begin working for different contacts.

Smart works with a well-connected buyer who values planning and
discretion. Strong works with a delivery boss who values speed and
intimidation.

The player switches between them to complete connected missions.

Both employers secretly take excessive portions of the profits. They also
give Smart and Strong conflicting information, attempting to create
mistrust between the two friends.

### Mission 15: Divided Loyalty

Smart discovers that Strong's employer arranged an attack on one of
Smart's buyers. Strong learns that Smart's employer has been reporting
selected competitors to the police.

The protagonists argue.

The player's previous choices influence the conversation, but they
ultimately discover that both employers are exploiting them. Their
friendship survives, although it is damaged.

They decide to work together again and build their own Grand
Bay-to-Roseau network.

### Mission 16: Heat at 100

A major Roseau sale goes wrong.

Heat reaches 100, additional police appear, and rival territory members
attempt to take the shipment. The objective is survival and escape rather
than defeating everyone.

Smart can identify safer exits and reduce losses. Strong can withstand
more damage and carry valuable inventory if the vehicle must be abandoned.

Their employer refuses to help and keeps the advance payment.

This becomes the point where they stop accepting exploitation as normal.

## Chapter Five — Bigger Water

A fictional captain offers an abstract Guadeloupe dispatch opportunity.

The player cannot travel to Guadeloupe or control the courier. It is an
investment and story system rather than a playable trafficking simulation.

The player must:

- Select a trusted recruited character.
- Choose how much product or legal produce to dispatch.
- Pay the fictional captain's €500 fee.
- Wait for the result.

Possible outcomes include:

- Successful payment in euros.
- Delay.
- Partial loss.
- Complete loss.
- Courier betrayal.
- Captain betrayal.
- Rival interference.

No real routes, evasion methods, or operational instructions are depicted.

The system tests whether Smart and Strong treat recruited people fairly or
exploit them like their former bosses did.

## Final Chapter — Up Iz Up

Boss K and the Roseau employers realize Smart and Strong are becoming
independent.

They cooperate temporarily to destroy the boys' network. Police pressure
increases, buyers disappear, a farm is attacked, and one recruited
character may betray them depending on loyalty.

Smart proposes a careful plan to expose the employers' betrayal and
preserve their legal farming operation. Strong wants to confront the
people responsible directly.

The final missions move between Grand Bay, Highland, and Roseau. Both
characters' abilities and switching are required.

## Possible endings

### The Farming Ending

Smart and Strong abandon the weed market after making enough money to
expand their legal farms.

They create a successful produce and transportation business, employ
local characters, and regain community respect.

They earn less money but obtain the safest and most stable future.

### The Independent Market Ending

They remove the exploitative employers and control their own
Grand Bay-to-Roseau operation.

They become wealthy and powerful, but police heat and rivalry never
disappear.

The final scene suggests they may eventually become the same kind of
people who once exploited them.

### The Community Ending

The boys combine legal farming, transportation, land ownership, and
selected high-risk activity while treating workers fairly.

They establish a cooperative-style network and invest in Grand Bay. This
is the most difficult ending because it requires money, reputation, crew
loyalty, and completing community missions.

It offers the best balance, although their past decisions still carry
consequences.

### The Broken Friendship Ending

If the player repeatedly favours money, betrayal, and one protagonist over
the other, Smart and Strong separate.

One controls Grand Bay while the other works in Roseau. They become
rivals, completing the story by becoming tools of the same system they
originally wanted to escape.
