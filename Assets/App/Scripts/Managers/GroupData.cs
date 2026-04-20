using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class GroupData
{
    #region Inspector Variables

    [Tooltip("Password the guest group uses to unlock content. Case-insensitive at runtime.")]
    public string password = "";

    [Tooltip("Display name shown in the personalized section, e.g. 'Famiglia Rossi'.")]
    public string groupDisplayName = "";

    [Tooltip("Individual guest names in this group, e.g. ['Marco', 'Sofia']. Used to pre-fill the RSVP form.")]
    public List<string> memberNames = new List<string>();

    [Tooltip("True for groups assigned to the wedding apartment. Enables breakfast preference in personalized section and RSVP.")]
    public bool hasBreakfastPref = false;

    [Tooltip("True for groups with a reserved apartment. Controls AlloggioPopup display.")]
    public bool hasApartment = false;

    [Tooltip("Apartment display name shown in AlloggioPopup, e.g. 'Appartamento Gialli'.")]
    public string apartmentName = "";

    [Tooltip("Full address of the apartment, e.g. 'Via Napoli 12, Città'.")]
    public string apartmentAddress = "";

    [Tooltip("Extra notes displayed in AlloggioPopup: check-in time, key pickup instructions, etc. One note per list entry.")]
    public List<string> apartmentNotes = new List<string>();

    #endregion
}
