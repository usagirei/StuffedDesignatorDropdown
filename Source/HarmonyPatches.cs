using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;
using RimWorld;
using UnityEngine;
using Harmony;
using System.Reflection;
using System.Reflection.Emit;

namespace StuffedDropdownDef
{
    [StaticConstructorOnStartup]
    public class HarmonyPatches
    {
        [HarmonyPatch(typeof(Designator_Dropdown), nameof(Designator_Dropdown.ProcessInput))]
        private static class Designator_Dropdown_ProcessInput
        {
            public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> oldInstrs)
            {
                var newInstrs = new List<CodeInstruction>(oldInstrs);
                var bypass = typeof(Designator_Dropdown_ProcessInput).GetMethod(nameof(PatchMenuItems), BindingFlags.Static | BindingFlags.Public);

                for(int i= 0; i < newInstrs.Count; i++)
                {
                    var instr = newInstrs[i];
                    if (instr.opcode != OpCodes.Newobj)
                        continue;

                    if (!(instr.operand is ConstructorInfo ctor))
                        continue;

                    if (ctor.DeclaringType != typeof(FloatMenu))
                        continue;

                    newInstrs.InsertRange(i,  new[]{
                        new CodeInstruction(OpCodes.Ldarg_1),
                        new CodeInstruction(OpCodes.Call, bypass) 
                    });
                    break;
                }

                return newInstrs.AsEnumerable();
            }

            public static List<FloatMenuOption> PatchMenuItems(List<FloatMenuOption> options, Event ev)
            {
                foreach(var opt in options)
                {
                    var oldAct = opt.action;
                    opt.action = delegate
                    {
                        oldAct();

                        var d = Find.DesignatorManager.SelectedDesignator;
                        if (d == null)
                            return;

                        d.ProcessInput(ev);
                    };
                }
                return options;
            }
        }

        static HarmonyPatches()
        {
            Log.Message("Stuffed Dropdowns Init");
            var harmony = HarmonyInstance.Create("StuffedDropdownDef");
            harmony.PatchAll(Assembly.GetExecutingAssembly());
        }
    }
}
