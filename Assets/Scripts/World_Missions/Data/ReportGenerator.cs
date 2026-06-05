using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;

/// <summary>
/// Generates a self-contained HTML therapist report from GameData.
/// Place a Chart.js minified build at Assets/Resources/chartjs.txt for offline use;
/// falls back to the jsDelivr CDN automatically if the file is absent.
/// </summary>
public static class ReportGenerator
{
    private const string CHARTJS_RESOURCE_PATH = "chartjs";
    private const string CHART_CDN_FALLBACK = "https://cdn.jsdelivr.net/npm/chart.js";

    public static string GenerateHTMLReport(GameData data, string outputFolderPath)
    {
        if (data == null)
        {
            Debug.LogWarning("[ReportGenerator] GameData is null — skipping HTML report.");
            return string.Empty;
        }

        string reportPath = Path.Combine(outputFolderPath, "informe_terapeuta.html");

        TextAsset chartJsAsset = Resources.Load<TextAsset>(CHARTJS_RESOURCE_PATH);
        string chartJsTag = chartJsAsset != null
            ? "<script>" + chartJsAsset.text + "</script>"
            : "<script src=\"" + CHART_CDN_FALLBACK + "\"></script>";

        try
        {
            File.WriteAllText(reportPath, BuildHTML(data, chartJsTag), Encoding.UTF8);
            Debug.Log($"[ReportGenerator] HTML report written to: {reportPath}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[ReportGenerator] Failed to write HTML report: {ex.Message}");
            return string.Empty;
        }

        return reportPath;
    }

    // ────────────────────────────────────────────────────────────────────────────
    // HTML assembly
    // ────────────────────────────────────────────────────────────────────────────

    private static string BuildHTML(GameData data, string chartJsTag)
    {
        int totalWrong = 0, totalQuestions = 0;
        if (data.dialogueAnalyticsSessions != null)
            foreach (var s in data.dialogueAnalyticsSessions) { totalWrong += s.wrongAnswers; totalQuestions += s.questionsAnswered; }

        string generatedDate = DateTime.Now.ToString("dd/MM/yyyy HH:mm");
        string playerName    = string.IsNullOrEmpty(data.playerName) ? "—" : data.playerName;
        string totalTime     = FormatTime(data.totalPlayedTime);
        int missionsCompleted = data.completedMissions?.Count ?? 0;
        string accuracyStr  = totalQuestions > 0
            ? $"{Mathf.RoundToInt((float)(totalQuestions - totalWrong) / totalQuestions * 100)}%"
            : "—";

        bool hasMissions  = data.dialogueAnalyticsSessions != null && data.dialogueAnalyticsSessions.Count > 0;
        bool hasInference = data.inferenceCategoryErrors    != null && data.inferenceCategoryErrors.Count > 0;

        var sb = new StringBuilder(32768);
        sb.AppendLine("<!DOCTYPE html>");
        sb.AppendLine("<html lang=\"es\">");
        sb.AppendLine("<head>");
        sb.AppendLine("  <meta charset=\"UTF-8\">");
        sb.AppendLine("  <meta name=\"viewport\" content=\"width=device-width,initial-scale=1\">");
        sb.AppendLine("  <title>Informe de Sesión — Gadgets Go</title>");
        sb.AppendLine(BuildCSS());
        sb.AppendLine("</head>");
        sb.AppendLine("<body>");

        // Header
        sb.AppendLine("<header>");
        sb.AppendLine("  <div class=\"hdr-inner\">");
        sb.AppendLine("    <div>");
        sb.AppendLine("      <h1>Informe de Sesión</h1>");
        sb.AppendLine("      <p class=\"subtitle\">Gadgets Go &mdash; Serious Game TEA</p>");
        sb.AppendLine("    </div>");
        sb.AppendLine("    <div class=\"hdr-meta\">");
        sb.AppendLine("      <div class=\"meta\"><span class=\"meta-lbl\">Paciente</span><span class=\"meta-val\">" + EscapeHtml(playerName) + "</span></div>");
        sb.AppendLine("      <div class=\"meta\"><span class=\"meta-lbl\">Generado</span><span class=\"meta-val\">" + generatedDate + "</span></div>");
        sb.AppendLine("    </div>");
        sb.AppendLine("  </div>");
        sb.AppendLine("</header>");

        sb.AppendLine("<main>");

        // Summary cards
        sb.AppendLine("<section>");
        sb.AppendLine("  <h2 class=\"sec-title\">Resumen de Sesión</h2>");
        sb.AppendLine("  <div class=\"cards\">");
        sb.AppendLine(Card("Tiempo Total",                   totalTime,                                    "blue"));
        sb.AppendLine(Card("Misiones Completadas",           missionsCompleted.ToString(),                  "green"));
        sb.AppendLine(Card("Precisión Global",          accuracyStr,                                  "purple"));
        sb.AppendLine(Card("Incomodidad Facial (eventos)",   data.totalFaceDiscomfortEvents.ToString(),     "red"));
        sb.AppendLine(Card("Respuestas Incorrectas",         totalWrong.ToString(),                         "orange"));
        sb.AppendLine(Card("Pistas Usadas",                  SumHints(data).ToString(),                    "yellow"));
        sb.AppendLine("  </div>");
        sb.AppendLine("</section>");

        // Mission charts
        if (hasMissions)
        {
            sb.AppendLine("<section>");
            sb.AppendLine("  <h2 class=\"sec-title\">Análisis por Misión</h2>");
            sb.AppendLine("  <div class=\"chart-grid\">");
            sb.AppendLine("    <div class=\"chart-card\"><h3>Rendimiento en Preguntas</h3><div class=\"chart-wrap\"><canvas id=\"cAnswers\"></canvas></div></div>");
            sb.AppendLine("    <div class=\"chart-card\"><h3>Seguimiento Ocular (segundos)</h3><div class=\"chart-wrap\"><canvas id=\"cEye\"></canvas></div></div>");
            sb.AppendLine("    <div class=\"chart-card\"><h3>Tiempo Promedio de Respuesta (s)</h3><div class=\"chart-wrap\"><canvas id=\"cRespTime\"></canvas></div></div>");
            sb.AppendLine("    <div class=\"chart-card\"><h3>Incomodidad Facial por Misión</h3><div class=\"chart-wrap\"><canvas id=\"cFace\"></canvas></div></div>");
            sb.AppendLine("  </div>");
            sb.AppendLine("</section>");
        }

        // Inference errors
        if (hasInference)
        {
            sb.AppendLine("<section>");
            sb.AppendLine("  <h2 class=\"sec-title\">Errores por Categoría de Inferencia Social</h2>");
            sb.AppendLine("  <div class=\"inf-container\">");
            sb.AppendLine("    <div class=\"chart-card\"><div class=\"chart-wrap\"><canvas id=\"cInference\"></canvas></div></div>");
            sb.AppendLine("    <div class=\"chart-card inf-legend\">");
            sb.AppendLine(BuildInferenceLegend(data.inferenceCategoryErrors));
            sb.AppendLine("    </div>");
            sb.AppendLine("  </div>");
            sb.AppendLine("</section>");
        }

        // Detail table
        if (hasMissions)
        {
            sb.AppendLine("<section>");
            sb.AppendLine("  <h2 class=\"sec-title\">Detalle por Misión</h2>");
            sb.AppendLine(BuildDetailTable(data.dialogueAnalyticsSessions));
            sb.AppendLine("</section>");
        }

        sb.AppendLine("</main>");
        sb.AppendLine("<footer>Gadgets Go VR &mdash; Informe generado el " + generatedDate + "</footer>");
        sb.AppendLine(chartJsTag);
        sb.AppendLine(BuildChartJS(data));
        sb.AppendLine("</body>");
        sb.AppendLine("</html>");
        return sb.ToString();
    }

    // ────────────────────────────────────────────────────────────────────────────
    // CSS
    // ────────────────────────────────────────────────────────────────────────────

    private static string BuildCSS() =>
@"<style>
*{box-sizing:border-box;margin:0;padding:0}
body{font-family:-apple-system,BlinkMacSystemFont,'Segoe UI',Roboto,sans-serif;background:#f0f2f5;color:#1a1a2e}
header{background:linear-gradient(135deg,#4361ee,#7b2d8b);color:#fff;padding:24px 32px}
.hdr-inner{max-width:1200px;margin:0 auto;display:flex;justify-content:space-between;align-items:center;flex-wrap:wrap;gap:16px}
h1{font-size:26px;font-weight:700}
.subtitle{font-size:13px;opacity:.8;margin-top:4px}
.hdr-meta{display:flex;gap:32px}
.meta{display:flex;flex-direction:column}
.meta-lbl{font-size:10px;opacity:.7;text-transform:uppercase;letter-spacing:.5px}
.meta-val{font-size:15px;font-weight:600;margin-top:2px}
main{max-width:1200px;margin:0 auto;padding:32px 16px;display:flex;flex-direction:column;gap:32px}
.sec-title{font-size:17px;font-weight:600;margin-bottom:16px;color:#2b2d42;border-left:4px solid #4361ee;padding-left:12px}
.cards{display:grid;grid-template-columns:repeat(auto-fill,minmax(175px,1fr));gap:14px}
.card{background:#fff;border-radius:12px;padding:20px;box-shadow:0 2px 8px rgba(0,0,0,.06);border-top:4px solid}
.card-lbl{display:block;font-size:10px;color:#6c757d;text-transform:uppercase;letter-spacing:.5px;margin-bottom:8px}
.card-val{display:block;font-size:28px;font-weight:700}
.blue{border-color:#4361ee}.blue .card-val{color:#4361ee}
.green{border-color:#06d6a0}.green .card-val{color:#0aab80}
.purple{border-color:#7b2d8b}.purple .card-val{color:#7b2d8b}
.red{border-color:#ef233c}.red .card-val{color:#ef233c}
.orange{border-color:#fb8500}.orange .card-val{color:#d97000}
.yellow{border-color:#ffd166}.yellow .card-val{color:#b07d00}
.chart-grid{display:grid;grid-template-columns:repeat(auto-fill,minmax(480px,1fr));gap:18px}
.chart-card{background:#fff;border-radius:12px;padding:22px;box-shadow:0 2px 8px rgba(0,0,0,.06)}
.chart-card h3{font-size:11px;color:#6c757d;margin-bottom:14px;font-weight:600;text-transform:uppercase;letter-spacing:.5px}
.chart-wrap{position:relative;height:260px}
.inf-container{display:grid;grid-template-columns:1fr 1fr;gap:18px;align-items:start}
.inf-legend{padding:8px 22px}
.inf-row{display:flex;align-items:center;gap:12px;padding:14px 0;border-bottom:1px solid #f0f2f5}
.inf-row:last-child{border-bottom:none}
.inf-dot{width:14px;height:14px;border-radius:50%;flex-shrink:0}
.inf-name{font-size:14px;font-weight:500;flex:1}
.inf-errors{font-size:22px;font-weight:700;color:#ef233c}
.tbl-wrap{overflow-x:auto}
table{width:100%;border-collapse:collapse;background:#fff;border-radius:12px;overflow:hidden;box-shadow:0 2px 8px rgba(0,0,0,.06)}
th{background:#4361ee;color:#fff;padding:11px 13px;text-align:left;font-size:10px;font-weight:600;text-transform:uppercase;letter-spacing:.5px;white-space:nowrap}
td{padding:10px 13px;font-size:12px;border-bottom:1px solid #f0f2f5;white-space:nowrap}
tr:last-child td{border-bottom:none}
tr:hover td{background:#f8f9ff}
.b{display:inline-block;padding:2px 7px;border-radius:99px;font-size:10px;font-weight:600}
.ok{background:#d4edda;color:#155724}.warn{background:#f8d7da;color:#721c24}.info{background:#cce5ff;color:#004085}
footer{text-align:center;padding:24px;color:#adb5bd;font-size:11px}
section{display:flex;flex-direction:column}
@media(max-width:800px){.chart-grid{grid-template-columns:1fr}.inf-container{grid-template-columns:1fr}.hdr-meta{flex-direction:column;gap:8px}}
</style>";

    // ────────────────────────────────────────────────────────────────────────────
    // Component builders
    // ────────────────────────────────────────────────────────────────────────────

    private static string Card(string label, string value, string colorClass) =>
        "    <div class=\"card " + colorClass + "\">\n" +
        "      <span class=\"card-lbl\">" + EscapeHtml(label) + "</span>\n" +
        "      <span class=\"card-val\">" + EscapeHtml(value) + "</span>\n" +
        "    </div>";

    private static string BuildInferenceLegend(List<InferenceCategoryErrorCounter> categories)
    {
        string[] colors = { "#4361ee", "#06d6a0", "#fb8500", "#ef233c" };
        string[] names  = { "Recepción del Mensaje", "Reconocimiento Emocional", "Predicción de Movimiento", "Comprensión Relacional" };

        var sb = new StringBuilder();
        foreach (var cat in categories)
        {
            int i      = (int)cat.category;
            string col = i < colors.Length ? colors[i] : "#adb5bd";
            string nm  = i < names.Length  ? names[i]  : EscapeHtml(cat.category.ToString());
            sb.AppendLine("      <div class=\"inf-row\">");
            sb.AppendLine("        <div class=\"inf-dot\" style=\"background:" + col + "\"></div>");
            sb.AppendLine("        <span class=\"inf-name\">" + nm + "</span>");
            sb.AppendLine("        <span class=\"inf-errors\">" + cat.errors + "</span>");
            sb.AppendLine("      </div>");
        }
        return sb.ToString();
    }

    private static string BuildDetailTable(List<DialogueAnalyticsEntry> sessions)
    {
        var sb = new StringBuilder();
        sb.AppendLine("<div class=\"tbl-wrap\">");
        sb.AppendLine("<table><thead><tr>");
        foreach (string h in new[] {
            "Misión", "Sesiones", "Tiempo (s)", "Preguntas", "Incorrectas", "Pistas",
            "T. Resp. Prom. (s)", "Enfocado (s)", "Distaído (s)", "Ev. Distracción",
            "Incomod. (ev.)", "Incomod. (s)"
        })
            sb.Append("<th>" + h + "</th>");
        sb.AppendLine("</tr></thead><tbody>");

        foreach (var s in sessions)
        {
            float eyeTotal = s.eyeFocusedSeconds + s.eyeDistractedSeconds;
            string focusPct = eyeTotal > 0.5f
                ? " <span class=\"b info\">" + Mathf.RoundToInt(s.eyeFocusedSeconds / eyeTotal * 100) + "%</span>"
                : "";
            string wrongBadge = s.wrongAnswers > 0
                ? "<span class=\"b warn\">" + s.wrongAnswers + "</span>"
                : "<span class=\"b ok\">0</span>";
            string faceBadge = s.faceDiscomfortEvents > 0
                ? "<span class=\"b warn\">" + s.faceDiscomfortEvents + "</span>"
                : "<span class=\"b ok\">0</span>";

            sb.AppendLine("<tr>");
            sb.AppendLine("<td><strong>" + EscapeHtml(s.dialogueId) + "</strong></td>");
            sb.AppendLine("<td>" + s.sessionsPlayed + "</td>");
            sb.AppendLine("<td>" + F1(s.timeSpent) + "</td>");
            sb.AppendLine("<td>" + s.questionsAnswered + "</td>");
            sb.AppendLine("<td>" + wrongBadge + "</td>");
            sb.AppendLine("<td>" + s.hintsRequested + "</td>");
            sb.AppendLine("<td>" + F1(s.AvgAnswerTimeSeconds) + "</td>");
            sb.AppendLine("<td>" + F1(s.eyeFocusedSeconds) + focusPct + "</td>");
            sb.AppendLine("<td>" + F1(s.eyeDistractedSeconds) + "</td>");
            sb.AppendLine("<td>" + s.eyeDistractionEvents + "</td>");
            sb.AppendLine("<td>" + faceBadge + "</td>");
            sb.AppendLine("<td>" + F1(s.faceDiscomfortSeconds) + "</td>");
            sb.AppendLine("</tr>");
        }

        sb.AppendLine("</tbody></table></div>");
        return sb.ToString();
    }

    // ────────────────────────────────────────────────────────────────────────────
    // Chart.js initialization script
    // ────────────────────────────────────────────────────────────────────────────

    private static string BuildChartJS(GameData data)
    {
        if (data.dialogueAnalyticsSessions == null || data.dialogueAnalyticsSessions.Count == 0)
            return "<script></script>";

        var ss = data.dialogueAnalyticsSessions;
        string lbl     = JsStrArr(ss.ConvertAll(s => TruncLabel(s.dialogueId)));
        string wrong   = JsIntArr(ss.ConvertAll(s => s.wrongAnswers));
        string hints   = JsIntArr(ss.ConvertAll(s => s.hintsRequested));
        string eyeFoc  = JsFltArr(ss.ConvertAll(s => s.eyeFocusedSeconds));
        string eyeDis  = JsFltArr(ss.ConvertAll(s => s.eyeDistractedSeconds));
        string resp    = JsFltArr(ss.ConvertAll(s => s.AvgAnswerTimeSeconds));
        string faceEv  = JsIntArr(ss.ConvertAll(s => s.faceDiscomfortEvents));
        string faceSec = JsFltArr(ss.ConvertAll(s => s.faceDiscomfortSeconds));

        // Inference arrays
        string infLbl = "[]", infDat = "[]", infCol = "[]";
        if (data.inferenceCategoryErrors != null && data.inferenceCategoryErrors.Count > 0)
        {
            string[] nm  = { "Recepción del Mensaje", "Reconocimiento Emocional", "Predicción de Movimiento", "Comprensión Relacional" };
            string[] col = { "#4361ee", "#06d6a0", "#fb8500", "#ef233c" };
            var ll = new List<string>(); var dd = new List<string>(); var cc = new List<string>();
            foreach (var cat in data.inferenceCategoryErrors)
            {
                int i = (int)cat.category;
                ll.Add("\"" + EscapeJs(i < nm.Length ? nm[i] : cat.category.ToString()) + "\"");
                dd.Add(cat.errors.ToString());
                cc.Add("\"" + (i < col.Length ? col[i] : "#adb5bd") + "\"");
            }
            infLbl = "[" + string.Join(",", ll) + "]";
            infDat = "[" + string.Join(",", dd) + "]";
            infCol = "[" + string.Join(",", cc) + "]";
        }

        bool hasInf = data.inferenceCategoryErrors != null && data.inferenceCategoryErrors.Count > 0;

        var sb = new StringBuilder();
        sb.AppendLine("<script>");
        sb.AppendLine("(function(){");
        sb.AppendLine("var lbl=" + lbl + ";");
        sb.AppendLine("var base={responsive:true,maintainAspectRatio:false,plugins:{legend:{position:'top'}}};");

        sb.AppendLine("new Chart('cAnswers',{type:'bar',data:{labels:lbl,datasets:[");
        sb.AppendLine("{label:'Respuestas Incorrectas',data:" + wrong + ",backgroundColor:'rgba(239,35,60,.75)',borderRadius:4},");
        sb.AppendLine("{label:'Pistas Usadas',data:" + hints + ",backgroundColor:'rgba(251,133,0,.75)',borderRadius:4}");
        sb.AppendLine("]},options:{...base,scales:{y:{beginAtZero:true,ticks:{stepSize:1}}}}});");

        sb.AppendLine("new Chart('cEye',{type:'bar',data:{labels:lbl,datasets:[");
        sb.AppendLine("{label:'Enfocado (s)',data:" + eyeFoc + ",backgroundColor:'rgba(6,214,160,.75)',borderRadius:4},");
        sb.AppendLine("{label:'Distaído (s)',data:" + eyeDis + ",backgroundColor:'rgba(239,35,60,.5)',borderRadius:4}");
        sb.AppendLine("]},options:{...base,scales:{x:{stacked:true},y:{stacked:true,beginAtZero:true}}}});");

        sb.AppendLine("new Chart('cRespTime',{type:'bar',data:{labels:lbl,datasets:[");
        sb.AppendLine("{label:'Tiempo Prom. Respuesta (s)',data:" + resp + ",backgroundColor:'rgba(67,97,238,.75)',borderRadius:4}");
        sb.AppendLine("]},options:{...base,scales:{y:{beginAtZero:true}}}});");

        sb.AppendLine("new Chart('cFace',{type:'bar',data:{labels:lbl,datasets:[");
        sb.AppendLine("{label:'Eventos Incomodidad',data:" + faceEv + ",backgroundColor:'rgba(123,45,139,.8)',borderRadius:4},");
        sb.AppendLine("{label:'Duración (s)',data:" + faceSec + ",backgroundColor:'rgba(123,45,139,.3)',borderRadius:4}");
        sb.AppendLine("]},options:{...base,scales:{y:{beginAtZero:true}}}});");

        if (hasInf)
        {
            sb.AppendLine("new Chart('cInference',{type:'doughnut',data:{labels:" + infLbl + ",datasets:[{data:" + infDat + ",backgroundColor:" + infCol + ",borderWidth:2}]},");
            sb.AppendLine("options:{responsive:true,maintainAspectRatio:false,plugins:{legend:{position:'bottom'}}}});");
        }

        sb.AppendLine("})();");
        sb.AppendLine("</script>");
        return sb.ToString();
    }

    // ────────────────────────────────────────────────────────────────────────────
    // Helpers
    // ────────────────────────────────────────────────────────────────────────────

    private static string JsStrArr(List<string> v) =>
        "[" + string.Join(",", v.ConvertAll(s => "\"" + EscapeJs(s) + "\"")) + "]";

    private static string JsIntArr(List<int> v) =>
        "[" + string.Join(",", v) + "]";

    private static string JsFltArr(List<float> v) =>
        "[" + string.Join(",", v.ConvertAll(f => f.ToString("F1", CultureInfo.InvariantCulture))) + "]";

    private static string TruncLabel(string s)
    {
        if (string.IsNullOrEmpty(s)) return "—";
        return s.Length > 22 ? s.Substring(0, 20) + "…" : s;
    }

    private static string FormatTime(float totalSeconds)
    {
        int h = (int)(totalSeconds / 3600);
        int m = (int)(totalSeconds % 3600 / 60);
        int s = (int)(totalSeconds % 60);
        return h > 0 ? $"{h}h {m:D2}m {s:D2}s" : $"{m}m {s:D2}s";
    }

    private static int SumHints(GameData data)
    {
        int total = 0;
        if (data.dialogueAnalyticsSessions != null)
            foreach (var s in data.dialogueAnalyticsSessions) total += s.hintsRequested;
        return total;
    }

    private static string F1(float f) => f.ToString("F1", CultureInfo.InvariantCulture);

    private static string EscapeHtml(string s)
    {
        if (string.IsNullOrEmpty(s)) return string.Empty;
        return s.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("\"", "&quot;");
    }

    private static string EscapeJs(string s)
    {
        if (string.IsNullOrEmpty(s)) return string.Empty;
        return s.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", "\\r");
    }
}
