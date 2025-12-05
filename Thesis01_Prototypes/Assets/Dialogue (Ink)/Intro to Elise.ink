INCLUDE Globals.ink

-> elise_intro 

=== elise_intro === 
{ greeted_elise:
    -> RETURN_MEETING
- else:
    -> FIRST_MEETING
}

=== FIRST_MEETING ===
~ greeted_elise = true
Narrated: You approach the elderly woman by the dock. 
Elise: You’re not from around here... I’m Elise. 
Elise: ... 
Elise: The tide’s quieter than usual today. 
Elise: Or maybe it's you bringing the silence with you.

+ Just passing through. 
    Elise: Mm. Most who pass through don’t look like they’ve come back. 
    -> MAIN_LOOP 

+ I’ve heard stories. My mother lived here once. 
    ~ mentionedMother = true 
    Elise: Hmph. There’s always someone who used to live here. 
    Elise: This town remembers the ones it wants to. The rest... fade. 
    -> MAIN_LOOP 

+ I’m looking for answers. Something my mother believed in. 
    ~ mentionedMother = true 
    Elise: Ah. So you’ve got blood in this soil too. 
    -> MAIN_LOOP 

=== RETURN_MEETING === 
Elise: Back again? Still chasing ghosts, or just watching the waves? 
Elise: ...
Narrated: She glances at the horizon, where the sea meets the fog.
Elise: I’ll see you soon again, I’m sure.
    -> END

=== MAIN_LOOP === 
+ You know this town well? 
    ELISE: I've seen it rise and fall. Mostly fall. 
    Elise: There used to be more fish, more boats, more poeple. Now there's just more rust. 
    Elise : Funny what people leave behind, thinking it will hold. 
    -> MAIN_LOOP 
    
+ Do you believe in the curse? 
    Elise: Believe in it? No. But I feel it in my joints. 
    Elise: A kind of ache in the land. The trees don’t sing the way they used to.
    Elise: Others say the land remembers what was taken. 
    -> MAIN_LOOP 
    
    + {mentionedMother} My mother tried to fix things once. 
        -> FIXING_MENTIONED 
    
    + {!mentionedMother} Someone I knew tried to fix things once. 
        -> FIXING_NOT_MENTIONED 
    
    -> MAIN_LOOP 
    
    + I should go. 
        Elise: Then go. The tide won’t wait for you. 
        -> END 
        
=== FIXING_MENTIONED === 
    Elise: Then she wasn’t the first to stand at the edge of this rot. 
    Elise: Be careful. This place forgets easily, but it doesn’t forgive. 
    -> MAIN_LOOP

=== FIXING_NOT_MENTIONED === 
    Elise: Then you’re not the first to stand at the edge of this rot. 
    Elise: Be careful. This place forgets easily, but it doesn’t forgive.  
    -> MAIN_LOOP 
    
=== END === 
-> DONE