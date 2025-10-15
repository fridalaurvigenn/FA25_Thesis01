-> elise_intro

=== elise_intro ===
You approach the elderly woman by the dock.
-> START

VAR greeted = false
VAR mentionedMother = false

=== START ===
{greeted == false:
    -> FIRST_MEETING
- else:
    -> RETURN_MEETING
}

=== FIRST_MEETING ===
~ greeted = true
Elderly Woman: You’re not from around here... I’m Elise.
ELISE: The tide’s quieter than usual today.
ELISE: Or maybe it's you bringing the silence with you.

+ Just passing through.
    ELISE: Mm. Most who pass through don’t look like they’ve come back.
    -> MAIN_LOOP

+ I’ve heard stories. My mother lived here once.
    ~ mentionedMother = true
    ELISE: Hmph. There’s always someone who used to live here.
    ELISE: This town remembers the ones it wants to. The rest... fade.
    -> MAIN_LOOP

+ I’m looking for answers. Something my mother believed in.
    ~ mentionedMother = true
    ELISE: Ah. So you’ve got blood in this soil too.
    -> MAIN_LOOP

=== RETURN_MEETING ===
ELISE: Back again? Still chasing ghosts, or just watching the waves?
-> MAIN_LOOP

=== MAIN_LOOP ===
+ You know this town well?
    ELISE: I've seen it rise and fall. Mostly fall.
    ELISE: There used to be more fish, more boats, more people. Now there's just more rust.
    ELISE: Funny what people leave behind, thinking it’ll hold.
    -> MAIN_LOOP

+ Do you believe in the curse?
    ELISE: Believe in it? No. But I feel it in my joints.
    ELISE: A kind of ache in the land. The trees don’t sing the way they used to.
    ELISE: Others say the land remembers what was taken.
    -> MAIN_LOOP

-> INCLUDE_FIX_OPTION

+ I should go.
    ELISE: Then go. The tide won’t wait.
    -> END

=== INCLUDE_FIX_OPTION ===
{mentionedMother:
    + My mother tried to fix things once.
        -> FIXING_MENTIONED
- else:
    + Someone in my family tried to fix things once.
        -> FIXING_NOT_MENTIONED
}
-> MAIN_LOOP

=== FIXING_MENTIONED ===
ELISE: Then she wasn’t the first to stand at the edge of this rot.
ELISE: Be careful. This place forgets easily, but it doesn’t forgive.
-> MAIN_LOOP

=== FIXING_NOT_MENTIONED ===
ELISE: Then you’re not the first to stand at the edge of this rot.
ELISE: Be careful. This place forgets easily, but it doesn’t forgive.
-> MAIN_LOOP

=== END ===
-> DONE