// <Trauma>
using Content.Goobstation.Common.Barks;
// </Trauma>
using System.Numerics;
using Content.Shared.Chat.Prototypes;
using Content.Shared.Humanoid.Markings;
using Content.Shared.Humanoid.Prototypes;
using Content.Shared.Preferences;
using Content.Shared.Preferences.Loadouts;
using Content.Shared.Roles;
using Content.Shared.Traits;
using Robust.Shared.Enums;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;
using Content.Shared._Nebula.DetailExaminable;
using Content.Shared._Nebula.ERP;

namespace Content.Shared.Humanoid;

/// <summary>
/// Holds all of the data for importing / exporting character profiles.
/// </summary>
[DataDefinition]
public sealed partial class HumanoidProfileExportV1
{
    [DataField]
    public string ForkId;

    [DataField]
    public int Version = 1;

    [DataField(required: true)]
    public HumanoidCharacterProfileV1 Profile = default!;

    public HumanoidProfileExportV2 ToV2()
    {
        return new()
        {
            ForkId = ForkId,
            Version = 2,
            Profile = Profile.ToV2()
        };
    }
}

[DataDefinition, Serializable]
public sealed partial class HumanoidCharacterProfileV1
{
    [DataField("_jobPriorities")]
    public Dictionary<ProtoId<JobPrototype>, JobPriority> JobPriorities = new();

    [DataField("_antagPreferences")]
    public HashSet<ProtoId<AntagPrototype>> AntagPreferences = new();

    [DataField("_traitPreferences")]
    public HashSet<ProtoId<TraitPrototype>> TraitPreferences = new();

    [DataField("_loadouts")]
    public Dictionary<string, RoleLoadout> Loadouts = new();

    [DataField]
    public string Name;

    [DataField]
    public ProtoId<SpeciesPrototype> Species;

    [DataField]
    public int Age;

    [DataField]
    public Sex Sex;

    [DataField]
    public Gender Gender;

    // Nebula start

    [DataField]
    public string CustomSpecies = string.Empty;

    [DataField]
    public float Width = 1f;

    [DataField]
    public float Height = 1f;

    [DataField]
    public DetailExaminableData? DetailExaminableData;

    [DataField("erpPreference")]
    public string? ERPPreference;

    [DataField]
    public string FlavorText = string.Empty;

    [DataField("oocFlavorText")]
    public string OOCFlavorText = string.Empty;

    [DataField]
    public string CharacterFlavorText = string.Empty;

    [DataField]
    public string GreenFlavorText = string.Empty;

    [DataField]
    public string YellowFlavorText = string.Empty;

    [DataField]
    public string RedFlavorText = string.Empty;

    [DataField]
    public string TagsFlavorText = string.Empty;

    [DataField]
    public string LinksFlavorText = string.Empty;

    [DataField("nsfwFlavorText")]
    public string NSFWFlavorText = string.Empty;

    [DataField("nsfwOOCFlavorText")]
    public string NSFWOOCFlavorText = string.Empty;

    [DataField("nsfwLinksFlavorText")]
    public string NSFWLinksFlavorText = string.Empty;

    [DataField("nsfwTagsFlavorText")]
    public string NSFWTagsFlavorText = string.Empty;
    // Nebula end

    [DataField]
    public HumanoidCharacterAppearanceV1 Appearance;

    [DataField]
    public SpawnPriorityPreference SpawnPriority;

    [DataField]
    public PreferenceUnavailableMode PreferenceUnavailable;

    /// <summary>
    /// Trauma - bark voice to use
    /// </summary>
    [DataField]
    public ProtoId<BarkPrototype> BarkVoice = HumanoidProfileSystem.DefaultBarkVoice;

    public HumanoidCharacterProfile ToV2()
    {
        // Nebula edit
        var erpStatus = Enum.TryParse<ERPStatus>(ERPPreference, out var status) ? status : ERPStatus.Ask;
        return new HumanoidCharacterProfile(Name, Species, Height, Width, CustomSpecies, DetailExaminableData ?? GetFlatDetailExaminableData(), erpStatus, Age, Sex, GetDefaultVoice(Species, Sex), Gender, Appearance.ToV2(Species), SpawnPriority, JobPriorities, PreferenceUnavailable, AntagPreferences, TraitPreferences, Loadouts,
            BarkVoice, new()); // Trauma
    }

    // In V2 voices are stored as a separate database entry, this picks the default for the species and sex, which would give the same voice as pre-nubody.
    private ProtoId<EmoteSoundsPrototype> GetDefaultVoice(ProtoId<SpeciesPrototype> species, Sex sex)
    {
        var prototypeManager = IoCManager.Resolve<IPrototypeManager>();

        // Nebula edit start
        if (!prototypeManager.TryIndex(species, out var speciesPrototye))
            return HumanoidCharacterProfile.DefaultVoice;

        return speciesPrototye.GetDefaultSounds(sex);
        // Nebula edit end
    }

    // Nebula start
    private DetailExaminableData GetFlatDetailExaminableData()
    {
        return new DetailExaminableData
        {
            FlavorText = FlavorText,
            OOCFlavorText = OOCFlavorText,
            CharacterFlavorText = CharacterFlavorText,
            GreenFlavorText = GreenFlavorText,
            YellowFlavorText = YellowFlavorText,
            RedFlavorText = RedFlavorText,
            TagsFlavorText = TagsFlavorText,
            LinksFlavorText = LinksFlavorText,
            NSFWFlavorText = NSFWFlavorText,
            NSFWOOCFlavorText = NSFWOOCFlavorText,
            NSFWLinksFlavorText = NSFWLinksFlavorText,
            NSFWTagsFlavorText = NSFWTagsFlavorText,
        };
    }
    // Nebula end
}


[DataDefinition, Serializable]
public sealed partial class HumanoidCharacterAppearanceV1
{
    [DataField("hair")]
    public string HairStyleId;

    [DataField]
    public Color HairColor;

    [DataField("facialHair")]
    public string FacialHairStyleId;

    [DataField]
    public Color FacialHairColor;

    [DataField]
    public Color EyeColor;

    [DataField]
    public Color SkinColor;

    [DataField]
    public List<Marking> Markings = new();

    public HumanoidCharacterAppearance ToV2(ProtoId<SpeciesPrototype> species)
    {
        var markingManager = IoCManager.Resolve<MarkingManager>();

        var incomingMarkings = Markings.ShallowClone();
        if (HairStyleId != string.Empty)
            incomingMarkings.Add(new(HairStyleId, new List<Color>() { HairColor }));
        if (FacialHairStyleId != string.Empty)
            incomingMarkings.Add(new(FacialHairStyleId, new List<Color>() { FacialHairColor }));

        return new HumanoidCharacterAppearance(EyeColor, SkinColor, markingManager.ConvertMarkings(incomingMarkings, species));
    }
}
