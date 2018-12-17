using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace _4BCC
{
    /// <summary>
    ///     simple peephole optimizer
    /// </summary>
    static class Optimizer
    {
        public static void Optimize(List<Mnemonic> _mnemonics)
        {
            var idx = 0;
            while (idx < _mnemonics.Count)
            {
                var m = _mnemonics[idx];

                var prevIdx = idx - 1;
                while (prevIdx > 0 && (_mnemonics[prevIdx].Command.StartsWith(";") || _mnemonics[prevIdx].Command == "STC"))
                    prevIdx--;
                var nextIdx = idx + 1;
                while (nextIdx < _mnemonics.Count - 1 && _mnemonics[nextIdx].Command.TrimStart().StartsWith(";"))
                    nextIdx++;
                var mprev = idx > 0 ? _mnemonics[prevIdx] : null;
                var mnext = idx < _mnemonics.Count - 1 ? _mnemonics[nextIdx] : null;

                // LDA follows STA
                if (m.Command == "LDA" && mprev?.Command == "STA")
                {
                    if (m.Param == mprev.Param)
                    {
                        _mnemonics.RemoveAt(idx);
                        continue;
                    }
                }

                // label directly after jump
                if (m.Command.StartsWith("J") && mnext != null)
                {
                    if (mnext.Command == m.Param + ":")
                    {
                        _mnemonics.RemoveAt(idx);
                        continue;
                    }
                }

                idx++;
            }

            idx = 0;
            while (idx < _mnemonics.Count)
            {
                var m = _mnemonics[idx];

                var prevIdx = idx - 1;
                while (prevIdx > 0 && _mnemonics[prevIdx].Command.TrimStart().StartsWith(";"))
                    prevIdx--;
                var nextIdx = idx + 1;
                while (nextIdx < _mnemonics.Count - 1 && _mnemonics[nextIdx].Command.TrimStart().StartsWith(";"))
                    nextIdx++;
                var mprev = idx > 0 ? _mnemonics[prevIdx] : null;
                var mnext = idx < _mnemonics.Count - 1 ? _mnemonics[nextIdx] : null;

                // STA to unused variable
                if (m.Command == "STA")
                {
                    var varName = m.Param;
                    var unUsed = false;

                    for (var idx1 = idx + 1; idx1 < _mnemonics.Count; idx1++)
                    {
                        var m1 = _mnemonics[idx1];
                        if (m1.Command == "; free " + varName)
                        {
                            _mnemonics.RemoveAt(idx1);
                            unUsed = true;
                            break;
                        }
                        if (m1.Command.StartsWith("; endproc") || m1.Param == varName)
                            break;
                    }
                    if (unUsed)
                    {
                        _mnemonics.RemoveAt(idx);
                        continue;
                    }
                }

                idx++;
            }
        }
    }
}
