using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace Druidkin
{
    /// Inspect tab for druids displaying wild shape progression, level, XP,
    /// power multiplier, unspent points, and the talent tree.
    public class ITab_Pawn_WildShape : ITab
    {
        private Vector2 scrollPosition = Vector2.zero;

        // Border colors by node category
        private static readonly Color FormColor = new Color(0.25f, 0.72f, 1f);
        private static readonly Color UpgradeColor = new Color(1f, 0.75f, 0.2f);
        private static readonly Color PerkColor = new Color(0.8f, 0.45f, 1f);

        // State colors
        private static readonly Color LearnedBg = new Color(0.16f, 0.32f, 0.16f, 0.9f);
        private static readonly Color LearnableBg = new Color(0.28f, 0.26f, 0.12f, 0.9f);
        private static readonly Color LockedBg = new Color(0.12f, 0.12f, 0.12f, 0.75f);

        private static readonly Color LearnedLine = new Color(0.4f, 0.85f, 0.4f, 0.8f);
        private static readonly Color LearnableLine = new Color(0.9f, 0.8f, 0.3f, 0.8f);
        private static readonly Color LockedLine = new Color(0.3f, 0.3f, 0.3f, 0.5f);

        public ITab_Pawn_WildShape()
        {
            size = new Vector2(640f, 470f);
            labelKey = "Druidkin_TabWildShape";
        }

        public override bool IsVisible
        {
            get
            {
                Pawn pawn = SelPawn;
                return pawn?.genes?.GetFirstGeneOfType<Gene_Druid>() != null;
            }
        }

        protected override void FillTab()
        {
            Pawn pawn = SelPawn;
            Gene_Druid gene = pawn?.genes?.GetFirstGeneOfType<Gene_Druid>();
            if (gene == null)
            {
                return;
            }

            Rect inRect = new Rect(0f, 0f, size.x, size.y).ContractedBy(16f);

            // ================= HEADER =================
            DrawHeader(inRect, pawn, gene);

            float headerHeight = 74f;
            Widgets.DrawLineHorizontal(inRect.x, inRect.y + headerHeight, inRect.width);

            // ================= TALENT TREE =================
            Rect treeOutRect = new Rect(inRect.x, inRect.y + headerHeight + 6f, inRect.width, inRect.height - headerHeight - 6f);
            DrawTalentTree(treeOutRect, gene);
        }

        private void DrawHeader(Rect inRect, Pawn pawn, Gene_Druid gene)
        {
            Text.Font = GameFont.Medium;
            string title = $"{pawn.LabelShortCap} · {"Druidkin_Level".Translate(gene.Level)}";
            Vector2 titleSize = Text.CalcSize(title);
            Widgets.Label(new Rect(inRect.x, inRect.y, titleSize.x, 32f), title);

            // XP Bar
            float barX = inRect.x + titleSize.x + 20f;
            float barW = inRect.width - (titleSize.x + 20f);
            Rect barRect = new Rect(barX, inRect.y + 4f, barW, 24f);

            if (gene.Level >= gene.MaxLevel)
            {
                Widgets.FillableBar(barRect, 1f, SolidColorMaterials.NewSolidColorTexture(new Color(0.2f, 0.6f, 0.2f)));
                Text.Font = GameFont.Small;
                Text.Anchor = TextAnchor.MiddleCenter;
                Widgets.Label(barRect, "Druidkin_MaxLevel".Translate());
                Text.Anchor = TextAnchor.UpperLeft;
            }
            else
            {
                float needed = gene.XpToNextLevel;
                float progress = Mathf.Clamp01(gene.Xp / Mathf.Max(1f, needed));
                Widgets.FillableBar(barRect, progress, SolidColorMaterials.NewSolidColorTexture(new Color(0.2f, 0.55f, 0.2f)));

                Text.Font = GameFont.Small;
                Text.Anchor = TextAnchor.MiddleCenter;
                string xpText = $"{gene.Xp:N0} / {needed:N0} XP";
                Widgets.Label(barRect, xpText);
                Text.Anchor = TextAnchor.UpperLeft;

                TooltipHandler.TipRegion(barRect, "Druidkin_XpBarTooltip".Translate(gene.Xp.ToString("N0"), needed.ToString("N0")));
            }

            // Subheader: Power multiplier & Unspent Points
            float subY = inRect.y + 36f;
            Text.Font = GameFont.Small;

            string powerStr = "Druidkin_PowerMultiplier".Translate(gene.MasteryFactor.ToString("0.00"));
            Widgets.Label(new Rect(inRect.x, subY, 200f, 26f), powerStr);

            int unspent = gene.UnspentPoints;
            string pointsStr = unspent == 1
                ? "Druidkin_UnspentPointsSingle".Translate(unspent)
                : "Druidkin_UnspentPointsPlural".Translate(unspent);

            if (unspent > 0)
            {
                GUI.color = new Color(1f, 0.85f, 0.3f);
            }
            else
            {
                GUI.color = new Color(0.7f, 0.7f, 0.7f);
            }

            Text.Anchor = TextAnchor.UpperRight;
            Widgets.Label(new Rect(inRect.x, subY, inRect.width, 26f), pointsStr);
            Text.Anchor = TextAnchor.UpperLeft;
            GUI.color = Color.white;
        }

        private void DrawTalentTree(Rect outRect, Gene_Druid gene)
        {
            List<WildShapeNodeDef> allNodes = DefDatabase<WildShapeNodeDef>.AllDefsListForReading;

            float cellSpacingX = 136f;
            float cellSpacingY = 72f;
            float startX = 14f;
            float startY = 32f;
            float nodeW = 120f;
            float nodeH = 54f;

            // Compute total view height and width
            float maxX = 0f;
            float maxY = 0f;
            foreach (WildShapeNodeDef node in allNodes)
            {
                float nx = startX + node.treePosition.x * cellSpacingX + nodeW;
                float ny = startY + node.treePosition.y * cellSpacingY + nodeH;
                if (nx > maxX) maxX = nx;
                if (ny > maxY) maxY = ny;
            }

            Rect viewRect = new Rect(0f, 0f, Mathf.Max(outRect.width - 16f, maxX + 20f), Mathf.Max(outRect.height - 10f, maxY + 20f));

            Widgets.BeginScrollView(outRect, ref scrollPosition, viewRect);

            // Column category labels
            Text.Font = GameFont.Small;
            GUI.color = new Color(0.6f, 0.6f, 0.6f);
            Widgets.Label(new Rect(startX, 6f, 160f, 22f), "Druidkin_CategoryForms".Translate());
            Widgets.Label(new Rect(startX + 3.2f * cellSpacingX, 6f, 160f, 22f), "Druidkin_CategoryPerks".Translate());
            GUI.color = Color.white;

            // Dictionary for node rects
            Dictionary<WildShapeNodeDef, Rect> nodeRects = new Dictionary<WildShapeNodeDef, Rect>();
            foreach (WildShapeNodeDef node in allNodes)
            {
                float x = startX + node.treePosition.x * cellSpacingX;
                float y = startY + node.treePosition.y * cellSpacingY;
                float w = node.treePosition.x >= 3f ? 135f : nodeW;
                nodeRects[node] = new Rect(x, y, w, nodeH);
            }

            // 1. Draw prerequisite lines
            foreach (WildShapeNodeDef node in allNodes)
            {
                if (node.prerequisites.NullOrEmpty())
                {
                    continue;
                }

                Rect toRect = nodeRects[node];
                Vector2 toCenter = toRect.center;

                bool isLearned = gene.LearnedNodes.Contains(node);
                bool canLearn = gene.CanLearn(node, out string _);

                foreach (WildShapeNodeDef prereq in node.prerequisites)
                {
                    if (prereq == null || !nodeRects.TryGetValue(prereq, out Rect fromRect))
                    {
                        continue;
                    }

                    Vector2 fromCenter = fromRect.center;

                    Color lineColor;
                    if (isLearned)
                    {
                        lineColor = LearnedLine;
                    }
                    else if (canLearn)
                    {
                        lineColor = LearnableLine;
                    }
                    else
                    {
                        lineColor = LockedLine;
                    }

                    Widgets.DrawLine(fromCenter, toCenter, lineColor, 2f);
                }
            }

            // 2. Draw nodes
            foreach (WildShapeNodeDef node in allNodes)
            {
                Rect nodeRect = nodeRects[node];
                DrawNode(nodeRect, node, gene);
            }

            Widgets.EndScrollView();
        }

        private void DrawNode(Rect rect, WildShapeNodeDef node, Gene_Druid gene)
        {
            bool isLearned = gene.LearnedNodes.Contains(node);
            string failReason = null;
            bool isLearnable = false;
            if (!isLearned)
            {
                isLearnable = gene.CanLearn(node, out failReason);
            }

            // Determine category border color
            Color borderColor;
            if (node.effects != null && node.effects.Any(e => e is NodeEffect_UnlockForm))
            {
                borderColor = FormColor;
            }
            else if (node.effects != null && node.effects.Any(e => e is NodeEffect_FormUpgrade))
            {
                borderColor = UpgradeColor;
            }
            else
            {
                borderColor = PerkColor;
            }

            // Background & border
            Color bgColor;
            if (isLearned)
            {
                bgColor = LearnedBg;
            }
            else if (isLearnable)
            {
                bgColor = LearnableBg;
                borderColor = Color.Lerp(borderColor, Color.white, 0.35f);
            }
            else
            {
                bgColor = LockedBg;
                borderColor = new Color(borderColor.r * 0.4f, borderColor.g * 0.4f, borderColor.b * 0.4f, 0.6f);
            }

            Widgets.DrawBoxSolidWithOutline(rect, bgColor, borderColor, isLearnable ? 2 : 1);

            // Text inside box
            Rect labelRect = new Rect(rect.x + 4f, rect.y + 4f, rect.width - 8f, 26f);
            Text.Font = GameFont.Tiny;
            Text.Anchor = TextAnchor.MiddleCenter;

            if (isLearned)
            {
                GUI.color = Color.white;
            }
            else if (isLearnable)
            {
                GUI.color = new Color(1f, 1f, 0.8f);
            }
            else
            {
                GUI.color = new Color(0.6f, 0.6f, 0.6f);
            }

            Widgets.Label(labelRect, node.LabelCap);

            // Status label (bottom half)
            Rect subRect = new Rect(rect.x + 4f, rect.y + 28f, rect.width - 8f, 22f);
            if (isLearned)
            {
                GUI.color = new Color(0.5f, 0.9f, 0.5f);
                Widgets.Label(subRect, "Druidkin_NodeStatusLearned".Translate());
            }
            else if (isLearnable)
            {
                GUI.color = new Color(1f, 0.85f, 0.3f);
                string costText = node.cost == 1
                    ? "Druidkin_NodeCostSingle".Translate(node.cost)
                    : "Druidkin_NodeCostPlural".Translate(node.cost);
                Widgets.Label(subRect, costText);
            }
            else
            {
                GUI.color = new Color(0.55f, 0.55f, 0.55f);
                if (gene.Level < node.minLevel)
                {
                    Widgets.Label(subRect, "Druidkin_RequiresLevelShort".Translate(node.minLevel));
                }
                else
                {
                    string costText = node.cost == 1
                        ? "Druidkin_NodeCostSingle".Translate(node.cost)
                        : "Druidkin_NodeCostPlural".Translate(node.cost);
                    Widgets.Label(subRect, costText);
                }
            }

            Text.Anchor = TextAnchor.UpperLeft;
            GUI.color = Color.white;

            // Tooltip
            string tip = BuildTooltip(node, gene, isLearned, isLearnable, failReason);
            TooltipHandler.TipRegion(rect, tip);

            // Click handling: learning opens a confirmation dialog
            if (Widgets.ButtonInvisible(rect))
            {
                if (isLearnable)
                {
                    string prompt = "Druidkin_ConfirmLearnPrompt".Translate(node.LabelCap, node.cost);
                    Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation(prompt, () => gene.Learn(node), destructive: false));
                }
                else if (!isLearned && !failReason.NullOrEmpty())
                {
                    Messages.Message(failReason, MessageTypeDefOf.RejectInput, historical: false);
                }
            }
        }

        private string BuildTooltip(WildShapeNodeDef node, Gene_Druid gene, bool isLearned, bool isLearnable, string failReason)
        {
            string text = node.LabelCap.Colorize(ColoredText.TipSectionTitleColor) + "\n";
            if (!node.description.NullOrEmpty())
            {
                text += node.description + "\n\n";
            }

            string costStr = node.cost == 1
                ? "Druidkin_NodeCostSingle".Translate(node.cost)
                : "Druidkin_NodeCostPlural".Translate(node.cost);
            text += $"{costStr}\n";

            if (node.minLevel > 0)
            {
                text += "Druidkin_NodeRequiresLevel".Translate(node.minLevel) + "\n";
            }

            if (!node.prerequisites.NullOrEmpty())
            {
                string prereqs = string.Join(", ", node.prerequisites.Select(p => p.LabelCap.Resolve()));
                text += "Druidkin_NodeRequiresPrereq".Translate(prereqs) + "\n";
            }

            text += "\n";
            if (isLearned)
            {
                text += "Druidkin_NodeStatusLearned".Translate().Colorize(Color.green);
            }
            else if (isLearnable)
            {
                text += "Druidkin_NodeClickToLearn".Translate().Colorize(new Color(1f, 0.85f, 0.3f));
            }
            else
            {
                text += ("Druidkin_NodeLocked".Translate() + ": " + failReason).Colorize(Color.red);
            }

            return text;
        }
    }
}
