using MioGame;
using MioModdingApi;
using MioModLoader;
namespace DisableInteractions;

public unsafe class DisableInteractions : Mod
{
    private static readonly Dictionary<string, string> hittableObjects = new(){
        { "Masocrystal_node", "Energy Crystal" },
        { "Mana_recipient_node", "Energy Flower" },
        { "Bomb", "Bomb" },
        { "Bumper_node", "Spring Flower" },
        { "Scrap_pile_node", "Junk Pile" },
        { "Breakable_node", "Breakable Wall" },
        { "Orb_node", "Splodge crystal" },
        { "Chocobon", "Crucible Spinner" },
    };

    private static ModMenuPage? modPage;
    private static readonly Dictionary<string, ToggleOption> isHittableOptions = [];
    private static ToggleOption? canHookCrystals;
    private static ToggleOption? canHookEnemies;
    private static ToggleOption? canHookSplodge;
    private static ToggleOption? canBeHitByBombs;
    private static ToggleOption? canSlingshotShields;

    public override void Initialize()
    {
        Hooks();
        ConnectMenus();
    }
    private void Hooks()
    {
        On.MioGame.GlobalFunctions.game.On_mio.find_hook_shot.Suffix += Find_hook_shot_suffix;
        On.MioGame.On_Combat_system.combatbox_overlap_test.Suffix += Combatbox_overlap_suffix;
    }

    public void ConnectMenus()
    {
        modPage = ModMenu.RegisterMod(this, "Boss Refight Mod", "Boss Refight Mod Options");

        foreach (string nodeType in hittableObjects.Keys)
        {
            string nodeName = hittableObjects[nodeType];
            isHittableOptions[nodeType] = modPage.RegisterToggle($"{nodeType}-is-hittable", $"{nodeName} objects are hittable", true, "Control whether this object can be hit by Mio's slashes");
        }

        canHookCrystals = modPage.RegisterToggle("can-hook-crystals", "Hairpin energy crystals", true, "Whether you can hairpin pre-placed energy crystals");
        canHookEnemies = modPage.RegisterToggle("can-hook-enemies", "Hairpin enemies", true, "Whether you can hairpin enemies (doesn't affect bosses)");
        canHookSplodge = modPage.RegisterToggle("can-hook-splodge", "Hairpin splodge crystals", true, "Whether you can hairpin energy crystals spawned by splodge or defrag");
        canBeHitByBombs = modPage.RegisterToggle("can-hit-bombs", "Can be hit by bombs", true, "Whether you can be hit by bombs. Disabling this also disables the ability to dodge bombs for energy refills");
        canSlingshotShields = modPage.RegisterToggle("can-slingshot-shields", "Can slingshot energy shields", true, "Whether slingshot projectiles can hit energy shields");
    }

    private void Combatbox_overlap_suffix(MioGame.Combat_system* self, Combat_hit* h)
    {
        try
        {
            if (h->result == Combat_hit.Result.Accepted || h->result == Combat_hit.Result.Blocked || h->result == Combat_hit.Result.Dodged)
            {
                string attacker = "not a struct";
                string victim = "not a struct";
                if (h->attacker_node->data.is_struct())
                {
                    attacker = Util.MioStringToString(*h->attacker_node->data.struct_name());
                }

                if (h->victim_node->data.is_struct())
                {
                    victim = Util.MioStringToString(*h->victim_node->data.struct_name());
                }

                LogMessage($"'{attacker}' just hit '{victim}'");

                if (attacker == "Explosion_node" && !(canBeHitByBombs?.Value ?? true))
                {
                    h->result = Combat_hit.Result.Rejected;
                }
                else if (attacker == "Orb_node" && victim == "Breakable_node" && !(canSlingshotShields?.Value ?? true))
                {
                    h->result = Combat_hit.Result.Rejected;
                }
                else if (isHittableOptions.TryGetValue(victim, out ToggleOption? value) && !value.Value)
                {
                    h->result = Combat_hit.Result.Rejected;
                }
            }
        }
        catch (Exception e)
        {
            LogMessage($"Error in Combatbox_overlap suffix: {e.Message}");
        }
    }

    private void Find_hook_shot_suffix(ref bool retBuffer, Mio* mio, Grid* g, Vec_float_2 unused, float max_length, Hook_hit* hit)
    {
        try
        {
            if (hit->node != null)
            {
                string nodeType = Util.MioStringToString(*(hit->node->data.struct_name()));
                LogMessage($"Found hairpin target of type: {nodeType}");

                if (nodeType == "Hookzone_node" && !(canHookCrystals?.Value ?? true))
                {
                    retBuffer = false;
                }
                else if (nodeType == "Hookpoint_node" && !(canHookEnemies?.Value ?? true))
                {
                    retBuffer = false;
                }
                else if (nodeType == "Orb_node" && !(canHookSplodge?.Value ?? true))
                {
                    retBuffer = false;
                }
            }
            else
            {
                LogMessage("No hairpin target found");
            }
        }
        catch (Exception e)
        {
            LogMessage($"Error in Find_hook_shot suffix: {e.Message}");
        }

    }
}
