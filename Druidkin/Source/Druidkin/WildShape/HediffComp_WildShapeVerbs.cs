using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using Verse;

namespace Druidkin
{
    public class HediffCompProperties_WildShapeVerbs : HediffCompProperties_VerbGiver
    {
        public HediffCompProperties_WildShapeVerbs()
        {
            compClass = typeof(HediffComp_WildShapeVerbs);
        }
    }

    /// Hosts the chosen animal's melee attacks on the colonist's own body.
    ///
    /// The vanilla comp takes its tools from XML, which cannot name an animal that is
    /// only chosen at shift time. Its Tools property is a sealed interface
    /// implementation and cannot be overridden - but it reads from props, and props is
    /// a per-comp field, so handing this instance its own copy redirects the vanilla
    /// verb machinery without reimplementing any of it.
    public class HediffComp_WildShapeVerbs : HediffComp_VerbGiver
    {
        private static readonly FieldInfo[] ToolFields =
            typeof(Tool).GetFields(BindingFlags.Instance | BindingFlags.Public);

        /// Called once the form is known: at shift time, and again after a save is
        /// loaded. The comp is built before the form is set, so it cannot do this itself.
        public void RefreshTools()
        {
            HediffCompProperties_VerbGiver shared = props as HediffCompProperties_VerbGiver;

            props = new HediffCompProperties_WildShapeVerbs
            {
                compClass = typeof(HediffComp_WildShapeVerbs),
                verbs = shared?.verbs,
                ownerTypeOverride = shared?.ownerTypeOverride,
                tools = DeriveTools()
            };

            verbTracker = new VerbTracker(this);
        }

        private List<Tool> DeriveTools()
        {
            List<Tool> tools = new List<Tool>();

            DruidkinAnimalFormDef form = (parent as Hediff_WildShapeForm)?.form;
            List<Tool> source = form?.Race?.tools;
            if (source.NullOrEmpty())
            {
                return tools;
            }

            float boost = Mathf.Max(0.01f, form.statBoostFactor);

            foreach (Tool original in source)
            {
                Tool tool = CopyOf(original);
                tool.power *= boost;

                // An animal's tools are pinned to body part groups its own body has -
                // FrontLeftPaw on a bear, for instance - and the colonist's body has no
                // such part, which would leave the tool permanently unusable. Dropping
                // the link makes the tool body-agnostic instead.
                tool.linkedBodyPartsGroup = null;
                tool.ensureLinkedBodyPartsGroupAlwaysUsable = false;

                tools.Add(tool);
            }

            return tools;
        }

        /// Copies field by field rather than listing them, so a Tool field added by a
        /// future RimWorld version is carried over instead of silently dropped.
        ///
        /// The copy matters: these tools are edited, and the originals belong to the
        /// animal's shared ThingDef. Mutating those would retune every wild animal of
        /// that species for the rest of the game.
        private static Tool CopyOf(Tool original)
        {
            Tool copy = new Tool();
            foreach (FieldInfo field in ToolFields)
            {
                field.SetValue(copy, field.GetValue(original));
            }

            return copy;
        }
    }
}
