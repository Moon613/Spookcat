using static Spookcat.Plugin;
public class ScareEverything {
    public static CreatureTemplate.Relationship.Type newRelation = CreatureTemplate.Relationship.Type.Afraid;   // there are a few different relationship values you can use here
    public static float intensity = 0.5f;     // this will be clamped to a 0-1 range, 1 influencing decision-making the most and 0 not at all
    ///<summary>This function makes setting the condition under which creatures will change their relation easier</summary>
    public static bool Condition(Creature? crit, ArtificialIntelligence self) {
        if (self.creature.personality.aggression < 0.55f && self.creature.personality.energy < 0.9f && self.creature.personality.bravery < 0.6f && self.creature.personality.dominance < 0.91f && crit != null && crit.Template.type == CreatureTemplate.Type.Slugcat && crit is Player player && player.slugcatStats.name == SpookyName) {
            return true;
        }
        else {
            return false;
        }
    }
    public static void Apply() {    // use this code by writing 'ScareEverything.Apply();' in the OnEnable of your mod
        On.ArtificialIntelligence.DynamicRelationship_CreatureRepresentation_AbstractCreature += (orig, self, rep, absCrit) => {
            Creature? trackedCreature = null;   // make sure that no null values are returned, and set the trackedCreature appropriatly
            if (rep != null) {
                trackedCreature = rep.representedCreature?.realizedCreature;
            }
            else if (absCrit != null) {
                trackedCreature = absCrit.realizedCreature;
            }
            if (Condition(trackedCreature, self)) {    // if a creature tries to access it's dynamic relationship, return a new one instead if consitions are met
                return new CreatureTemplate.Relationship(newRelation, intensity);
            }
            return orig(self, rep, absCrit);
        };

        On.ArtificialIntelligence.StaticRelationship += (orig, self, otherCreature) => {
            if (otherCreature.realizedCreature != null && Condition(otherCreature.realizedCreature, self)) {    // if a creature skips calling DynamicRelationship in favor of using a Static one, catch it here and make sure to still return a new relationship
                return new CreatureTemplate.Relationship(newRelation, intensity);
            }
            return orig(self, otherCreature);
        };

        // from here down, it is all maunally returning a new relationship for specific creature's AI dynamic relationship hook. Some return values before asking the functions above and there is no universal hook to use, so this unfortunatly is needed.
        // some creatures may not be included because they already fear the player, or I just missed them.
        // if you need to add one, literally just copy-paste the entirety of one hook, and change the 'CreaturetypeAI' to a new creature's AI class. ex: DaddyAI -> SnailAI. Nothing else needs to be done
        // this probably works with modded creatures as long as you have included a mod reference to their mod.

        #region Manually Returning New Relationships

        On.BigEelAI.IUseARelationshipTracker_UpdateDynamicRelationship += (orig, self, dRelation) => {
            Creature? trackedCreature = dRelation?.trackerRep?.representedCreature?.realizedCreature;
            if (Condition(trackedCreature, self)) {
                return new CreatureTemplate.Relationship(newRelation, intensity);
            }
            return orig(self, dRelation);
        };

        On.BigNeedleWormAI.IUseARelationshipTracker_UpdateDynamicRelationship += (orig, self, dRelation) => {
            Creature? trackedCreature = dRelation?.trackerRep?.representedCreature?.realizedCreature;
            if (Condition(trackedCreature, self) && !(dRelation?.state is NeedleWormAI.NeedleWormTrackState state && state.holdingChild)) {
                return new CreatureTemplate.Relationship(newRelation, intensity);
            }
            return orig(self, dRelation);
        };

        On.BigSpiderAI.IUseARelationshipTracker_UpdateDynamicRelationship += (orig, self, dRelation) => {
            Creature? trackedCreature = dRelation?.trackerRep?.representedCreature?.realizedCreature;
            if (Condition(trackedCreature, self)) {
                return new CreatureTemplate.Relationship(newRelation, intensity);
            }
            return orig(self, dRelation);
        };

        On.CentipedeAI.IUseARelationshipTracker_UpdateDynamicRelationship += (orig, self, dRelation) => {
            Creature? trackedCreature = dRelation?.trackerRep?.representedCreature?.realizedCreature;
            if (Condition(trackedCreature, self)) {
                return new CreatureTemplate.Relationship(newRelation, intensity);
            }
            return orig(self, dRelation);
        };

        On.CicadaAI.IUseARelationshipTracker_UpdateDynamicRelationship += (orig, self, dRelation) => {
            Creature? trackedCreature = dRelation?.trackerRep?.representedCreature?.realizedCreature;
            if (Condition(trackedCreature, self)) {
                return new CreatureTemplate.Relationship(newRelation, intensity);
            }
            return orig(self, dRelation);
        };

        On.DaddyAI.IUseARelationshipTracker_UpdateDynamicRelationship += (orig, self, dRelation) => {
            Creature? trackedCreature = dRelation?.trackerRep?.representedCreature?.realizedCreature;
            if (Condition(trackedCreature, self)) {
                return new CreatureTemplate.Relationship(newRelation, intensity);
            }
            return orig(self, dRelation);
        };

        On.DeerAI.IUseARelationshipTracker_UpdateDynamicRelationship += (orig, self, dRelation) => {
            Creature? trackedCreature = dRelation?.trackerRep?.representedCreature?.realizedCreature;
            if (Condition(trackedCreature, self)) {
                return new CreatureTemplate.Relationship(newRelation, intensity);
            }
            return orig(self, dRelation);
        };

        On.DropBugAI.IUseARelationshipTracker_UpdateDynamicRelationship += (orig, self, dRelation) => {
            Creature? trackedCreature = dRelation?.trackerRep?.representedCreature?.realizedCreature;
            if (Condition(trackedCreature, self)) {
                return new CreatureTemplate.Relationship(newRelation, intensity);
            }
            return orig(self, dRelation);
        };

        On.EggBugAI.IUseARelationshipTracker_UpdateDynamicRelationship += (orig, self, dRelation) => {
            Creature? trackedCreature = dRelation?.trackerRep?.representedCreature?.realizedCreature;
            if (Condition(trackedCreature, self)) {
                return new CreatureTemplate.Relationship(newRelation, intensity);
            }
            return orig(self, dRelation);
        };

        On.JetFishAI.IUseARelationshipTracker_UpdateDynamicRelationship += (orig, self, dRelation) => {
            Creature? trackedCreature = dRelation?.trackerRep?.representedCreature?.realizedCreature;
            if (Condition(trackedCreature, self)) {
                return new CreatureTemplate.Relationship(newRelation, intensity);
            }
            return orig(self, dRelation);
        };

        On.LizardAI.IUseARelationshipTracker_UpdateDynamicRelationship += (orig, self, dRelation) => {
            Creature? trackedCreature = dRelation?.trackerRep?.representedCreature?.realizedCreature;
            if (Condition(trackedCreature, self)) {
                return new CreatureTemplate.Relationship(newRelation, intensity);
            }
            return orig(self, dRelation);
        };

        On.MirosBirdAI.IUseARelationshipTracker_UpdateDynamicRelationship += (orig, self, dRelation) => {
            Creature? trackedCreature = dRelation?.trackerRep?.representedCreature?.realizedCreature;
            if (Condition(trackedCreature, self)) {
                return new CreatureTemplate.Relationship(newRelation, intensity);
            }
            return orig(self, dRelation);
        };

        On.ScavengerAI.IUseARelationshipTracker_UpdateDynamicRelationship += (orig, self, dRelation) => {
            Creature? trackedCreature = dRelation?.trackerRep?.representedCreature?.realizedCreature;
            if (Condition(trackedCreature, self)) {
                return new CreatureTemplate.Relationship(newRelation, intensity);
            }
            return orig(self, dRelation);
        };

        On.TempleGuardAI.IUseARelationshipTracker_UpdateDynamicRelationship += (orig, self, dRelation) => {
            Creature? trackedCreature = dRelation?.trackerRep?.representedCreature?.realizedCreature;
            if (Condition(trackedCreature, self)) {
                return new CreatureTemplate.Relationship(newRelation, intensity);
            }
            return orig(self, dRelation);
        };

        On.TentaclePlantAI.UpdateDynamicRelationship += (orig, self, dRelation) => {
            Creature? trackedCreature = dRelation?.trackerRep?.representedCreature?.realizedCreature;
            if (Condition(trackedCreature, self)) {
                return new CreatureTemplate.Relationship(newRelation, intensity);
            }
            return orig(self, dRelation);
        };

        On.VultureAI.IUseARelationshipTracker_UpdateDynamicRelationship += (orig, self, dRelation) => {
            Creature? trackedCreature = dRelation?.trackerRep?.representedCreature?.realizedCreature;
            if (Condition(trackedCreature, self)) {
                return new CreatureTemplate.Relationship(newRelation, intensity);
            }
            return orig(self, dRelation);
        };

        On.MoreSlugcats.InspectorAI.IUseARelationshipTracker_UpdateDynamicRelationship += (orig, self, dRelation) => {
            Creature? trackedCreature = dRelation?.trackerRep?.representedCreature?.realizedCreature;
            if (Condition(trackedCreature, self)) {
                return new CreatureTemplate.Relationship(newRelation, intensity);
            }
            return orig(self, dRelation);
        };

        On.MoreSlugcats.SlugNPCAI.IUseARelationshipTracker_UpdateDynamicRelationship += (orig, self, dRelation) => {
            Creature? trackedCreature = dRelation?.trackerRep?.representedCreature?.realizedCreature;
            if (Condition(trackedCreature, self)) {
                return new CreatureTemplate.Relationship(newRelation, intensity);
            }
            return orig(self, dRelation);
        };

        On.MoreSlugcats.StowawayBugAI.IUseARelationshipTracker_UpdateDynamicRelationship += (orig, self, dRelation) => {
            Creature? trackedCreature = dRelation?.trackerRep?.representedCreature?.realizedCreature;
            if (Condition(trackedCreature, self)) {
                return new CreatureTemplate.Relationship(newRelation, intensity);
            }
            return orig(self, dRelation);
        };

        On.MoreSlugcats.YeekAI.IUseARelationshipTracker_UpdateDynamicRelationship += (orig, self, dRelation) => {
            Creature? trackedCreature = dRelation?.trackerRep?.representedCreature?.realizedCreature;
            if (Condition(trackedCreature, self)) {
                return new CreatureTemplate.Relationship(newRelation, intensity);
            }
            return orig(self, dRelation);
        };
        #endregion
    }
}