using System;
using System.Linq;
using System.Text;
using System.Windows;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using VMS.TPS.Common.Model.API;
using VMS.TPS.Common.Model.Types;
using VMS.TPS.VisualScripting.ElementInterface;

// TODO: Replace the following version attributes by creating AssemblyInfo.cs. You can do this in the properties of the Visual Studio project.
[assembly: AssemblyVersion("1.0.0.1")]
[assembly: AssemblyFileVersion("1.0.0.1")]
[assembly: AssemblyInformationalVersion("1.0")]

namespace PlanCheckAP
{
    // TODO: Replace the existing class name with your own class name.
    public class PlanCheckPackElement : VisualScriptElement
    {
        public PlanCheckPackElement() { }
        public PlanCheckPackElement(IVisualScriptElementRuntimeHost host) { }

        public override bool RequiresRuntimeConsole { get { return false; } }
        public override bool RequiresDatabaseModifications { get { return false; } }


        [ActionPackExecuteMethod]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public List<PlanCheck> Execute(PlanSetup ps)
        {
            // TODO: Add your code here.
            List<PlanCheck> Checks = new List<PlanCheck>();
            if(m_options["Plan Type"] == "IMRT")
            {
                var doseCheck = new PlanCheck { Check = "Max Dose Inside Target" };
                var target = ps.StructureSet.Structures.FirstOrDefault(x => x.Id == ps.TargetVolumeID);
                doseCheck.Pass = target.IsPointInsideSegment(ps.Dose.DoseMax3DLocation);
                doseCheck.Value = doseCheck.Pass ? $"Max Dose: {ps.Dose.DoseMax3D} inside {target.Id}" : $"Max Dose: {ps.Dose.DoseMax3D} not in {target.Id}";
                Checks.Add(doseCheck);

                var muCheck = new PlanCheck { Check = "LMC MU" };
                var fieldMU = ps.Beams.FirstOrDefault().Meterset.Value;
                double maxMU = 0.0;
                double lostMU = 0.0;
                foreach(var line in ps.Beams.FirstOrDefault().CalculationLogs.FirstOrDefault(x=>x.Category == "LMC").MessageLines)
                {
                    if (line.Contains("LostMUFactor"))
                    {
                        lostMU = Convert.ToDouble(line.Split(' ').Last());
                    }
                    if (line.Contains("Maximum MUs"))
                    {
                        maxMU = Convert.ToDouble(line.Split(' ').Last());
                    }
                }
                var lmcMU = maxMU * lostMU;
                muCheck.Value = $"Beam MU: {fieldMU:F1}, LMC MU: {lmcMU:F1}";
                muCheck.Pass = Math.Abs((lmcMU - fieldMU) / fieldMU) < 0.05;
                Checks.Add(muCheck);
            }
            return Checks;
        }

        public override string DisplayName
        {
            get
            {
                // TODO: Replace "Element Name" with the name that you want to be displayed in the Visual Scripting UI.
                return "Plan Checks";
            }
        }

        IDictionary<string, string> m_options = new Dictionary<string, string>();
        public override void SetOption(string key, string value)
        {
            m_options.Add(key, value);
        }

        public override IEnumerable<KeyValuePair<string, IEnumerable<string>>> AllowedOptions
        {
            get
            {
                return new KeyValuePair<string, IEnumerable<string>>[] {
            new KeyValuePair<string, IEnumerable<string>>("Plan Type", new string[] { "IMRT", "VMAT" })
          };
            }
        }
    }
    public class PlanCheck
    {
        public string Check { get; set; }
        public string Value { get; set; }
        public bool Pass { get; set; }
    }
}
