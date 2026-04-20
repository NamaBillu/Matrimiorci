// RSVPSheet.gs — Google Apps Script
// ============================================================
// SETUP:
// 1. Open your RSVP Google Sheet → Extensions → Apps Script
// 2. Paste this entire file into Code.gs (replace existing content)
// 3. Deploy → New deployment → Web app
//    - Execute as: Me
//    - Who has access: Anyone  ← CRITICAL: must be "Anyone", not authenticated
//      (if set to "Anyone with link" or authenticated, Unity receives a 401/redirect)
// 4. Copy the deployment URL into Unity Inspector:
//    PopupManager prefab → RSVPPopup → App Script Url
// ============================================================

function doGet(e) {
  try {
    var sheet = SpreadsheetApp.getActiveSpreadsheet().getActiveSheet();

    var code      = e.parameter["code"]      || "";
    var notes     = e.parameter["notes"]     || "";
    var breakfast = e.parameter["breakfast"] || "";

    // Build row: [timestamp, groupCode, guest0Name, guest0Attendance, guest0Meal, ..., breakfast, notes]
    var row = [new Date(), sanitize(code)];

    var i = 0;
    while (e.parameter["guest" + i + "Name"]) {
      var name       = e.parameter["guest" + i + "Name"]       || "";
      var attendance = e.parameter["guest" + i + "Attendance"] || "no";
      var meal       = e.parameter["guest" + i + "Meal"]       || "non specificato";

      row.push(sanitize(name));
      row.push(attendance);           // "si" / "no" — no formula injection risk
      row.push(sanitize(meal));
      i++;
    }

    row.push(sanitize(breakfast));
    row.push(sanitize(notes));

    sheet.appendRow(row);

    // Return plain text "ok" — UnityWebRequest.Result.Success checks HTTP 200
    return ContentService
      .createTextOutput("ok")
      .setMimeType(ContentService.MimeType.TEXT);

  } catch (err) {
    return ContentService
      .createTextOutput("error: " + err.message)
      .setMimeType(ContentService.MimeType.TEXT);
  }
}

/**
 * Prevents Google Sheets formula injection.
 * Values starting with =, +, -, or @ are prefixed with an apostrophe.
 * The apostrophe is hidden by Sheets and prevents formula evaluation.
 *
 * @param {*} value - The parameter value to sanitize.
 * @returns {string} Safe string value for appendRow().
 */
function sanitize(value) {
  if (typeof value !== "string") return String(value);
  var formulaPrefixes = ["=", "+", "-", "@"];
  for (var i = 0; i < formulaPrefixes.length; i++) {
    if (value.charAt(0) === formulaPrefixes[i]) {
      return "'" + value;
    }
  }
  return value;
}
