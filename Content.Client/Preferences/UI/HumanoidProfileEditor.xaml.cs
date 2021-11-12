using System;
using System.Collections.Generic;
using System.Linq;
using Content.Client.CharacterAppearance;
using Content.Client.Lobby.UI;
using Content.Client.Message;
using Content.Client.Stylesheets;
using Content.Shared.CharacterAppearance;
using Content.Shared.CharacterAppearance.Systems;
using Content.Shared.GameTicking;
using Content.Shared.Preferences;
using Content.Shared.Roles;
using Robust.Client.AutoGenerated;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.XAML;
using Robust.Client.Utility;
using Robust.Shared.Enums;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Log;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using Robust.Shared.Utility;
using static Robust.Client.UserInterface.Controls.BoxContainer;

namespace Content.Client.Preferences.UI
{
    public class HighlightedContainer : PanelContainer
    {
        public HighlightedContainer()
        {
            PanelOverride = new StyleBoxFlat()
            {
                BackgroundColor = new Color(47, 47, 53),
                ContentMarginTopOverride = 10,
                ContentMarginBottomOverride = 10,
                ContentMarginLeftOverride = 10,
                ContentMarginRightOverride = 10
            };
        }
    }

    [GenerateTypedNameReferences]
    public partial class HumanoidProfileEditor : Control
    {
        private LineEdit _ageEdit => CAgeEdit;
        private LineEdit _nameEdit => CNameEdit;
        private Button _nameRandomButton => CNameRandomize;
        private Button _randomizeEverythingButton => CRandomizeEverything;
        private RichTextLabel _warningLabel => CWarningLabel;
        private readonly IClientPreferencesManager _preferencesManager;
        private Button _saveButton => CSaveButton;
        private Button _sexFemaleButton => CSexFemale;
        private Button _sexMaleButton => CSexMale;
        private OptionButton _genderButton => CPronounsButton;
        private Slider _skinColor => CSkin;
        private OptionButton _clothingButton => CClothingButton;
        private OptionButton _backpackButton => CBackpackButton;
        private HairStylePicker _hairPicker => CHairStylePicker;
        private HairStylePicker _facialHairPicker => CFacialHairPicker;
        private EyeColorPicker _eyesPicker => CEyeColorPicker;

        private TabContainer _tabContainer => CTabContainer;
        private BoxContainer _jobList => CJobList;
        private BoxContainer _antagList => CAntagList;
        private readonly List<JobPrioritySelector> _jobPriorities;
        private OptionButton _preferenceUnavailableButton => CPreferenceUnavailableButton;
        private readonly Dictionary<string, BoxContainer> _jobCategories;

        private readonly List<AntagPreferenceSelector> _antagPreferences;

        private readonly IEntity _previewDummy;
        private Control _previewSpriteControl => CSpriteViewFront;
        private Control _previewSpriteSideControl => CSpriteViewSide;
        private readonly SpriteView _previewSprite;
        private readonly SpriteView _previewSpriteSide;

        private bool _isDirty;
        private bool _needUpdatePreview;
        public int CharacterSlot;
        public HumanoidCharacterProfile? Profile;

        public event Action<HumanoidCharacterProfile, int>? OnProfileChanged;

        public HumanoidProfileEditor(IClientPreferencesManager preferencesManager, IPrototypeManager prototypeManager,
            IEntityManager entityManager)
        {
            RobustXamlLoader.Load(this);
            _random = IoCManager.Resolve<IRobustRandom>();
            _prototypeManager = prototypeManager;

            _preferencesManager = preferencesManager;

            #region Left

            #region Randomize

            #endregion Randomize

            #region Name

            _nameEdit.OnTextChanged += args => { SetName(args.Text); };
            _nameRandomButton.OnPressed += args => RandomizeName();
            _randomizeEverythingButton.OnPressed += args => { RandomizeEverything(); };
            _warningLabel.SetMarkup($"[color=red]{Loc.GetString("humanoid-profile-editor-naming-rules-warning")}[/color]");

            #endregion Name

            #region Appearance

            _tabContainer.SetTabTitle(0, Loc.GetString("humanoid-profile-editor-appearance-tab"));

            #region Sex

            var sexButtonGroup = new ButtonGroup();

            _sexMaleButton.Group = sexButtonGroup;
            _sexMaleButton.OnPressed += args =>
            {
                SetSex(Sex.Male);
                if (Profile?.Gender == Gender.Female)
                {
                    SetGender(Gender.Male);
                    UpdateGenderControls();
                }
            };

            _sexFemaleButton.Group = sexButtonGroup;
            _sexFemaleButton.OnPressed += _ =>
            {
                SetSex(Sex.Female);

                if (Profile?.Gender == Gender.Male)
                {
                    SetGender(Gender.Female);
                    UpdateGenderControls();
                }
            };

            #endregion Sex

            #region Age

            _ageEdit.OnTextChanged += args =>
            {
                if (!int.TryParse(args.Text, out var newAge))
                    return;
                SetAge(newAge);
            };

            #endregion Age

            #region Gender

            _genderButton.AddItem(Loc.GetString("humanoid-profile-editor-pronouns-male-text"), (int) Gender.Male);
            _genderButton.AddItem(Loc.GetString("humanoid-profile-editor-pronouns-female-text"), (int) Gender.Female);
            _genderButton.AddItem(Loc.GetString("humanoid-profile-editor-pronouns-epicene-text"), (int) Gender.Epicene);
            _genderButton.AddItem(Loc.GetString("humanoid-profile-editor-pronouns-neuter-text"), (int) Gender.Neuter);

            _genderButton.OnItemSelected += args =>
            {
                _genderButton.SelectId(args.Id);
                SetGender((Gender) args.Id);
            };

            #endregion Gender

            #region Skin

            // 0 - 100, 0 being gold/yellowish and 100 being dark
            // HSV based
            //
            // 0 - 20 changes the hue
            // 20 - 100 changes the value
            // 0 is 45 - 20 - 100
            // 20 is 25 - 20 - 100
            // 100 is 25 - 100 - 20
            _skinColor.OnValueChanged += range =>
            {
                if (Profile is null)
                    return;

                int rangeOffset = (int) range.Value - 20;

                float hue = 25;
                float sat = 20;
                float val = 100;

                if (rangeOffset < 0)
                {
                    hue += Math.Abs(rangeOffset);
                }
                else if (rangeOffset > 0)
                {
                    sat += rangeOffset;
                    val -= rangeOffset;
                }

                var color = Color.FromHsv(new Vector4(hue / 360, sat / 100, val / 100, 1.0f));

                Profile = Profile.WithCharacterAppearance(
                    Profile.Appearance.WithSkinColor(color));
                IsDirty = true;
            };

            #endregion

            #region Hair

            _hairPicker.Populate();

            _hairPicker.OnHairStylePicked += newStyle =>
            {
                if (Profile is null)
                    return;
                Profile = Profile.WithCharacterAppearance(
                    Profile.Appearance.WithHairStyleName(newStyle));
                IsDirty = true;
            };

            _hairPicker.OnHairColorPicked += newColor =>
            {
                if (Profile is null)
                    return;
                Profile = Profile.WithCharacterAppearance(
                    Profile.Appearance.WithHairColor(newColor));
                IsDirty = true;
            };

            _facialHairPicker.Populate();

            _facialHairPicker.OnHairStylePicked += newStyle =>
            {
                if (Profile is null)
                    return;
                Profile = Profile.WithCharacterAppearance(
                    Profile.Appearance.WithFacialHairStyleName(newStyle));
                IsDirty = true;
            };

            _facialHairPicker.OnHairColorPicked += newColor =>
            {
                if (Profile is null)
                    return;
                Profile = Profile.WithCharacterAppearance(
                    Profile.Appearance.WithFacialHairColor(newColor));
                IsDirty = true;
            };

            #endregion Hair

            #region Clothing

            _clothingButton.AddItem(Loc.GetString("humanoid-profile-editor-preference-jumpsuit"), (int) ClothingPreference.Jumpsuit);
            _clothingButton.AddItem(Loc.GetString("humanoid-profile-editor-preference-jumpskirt"), (int) ClothingPreference.Jumpskirt);

            _clothingButton.OnItemSelected += args =>
            {
                _clothingButton.SelectId(args.Id);
                SetClothing((ClothingPreference) args.Id);
            };

            #endregion Clothing

            #region Backpack

            _backpackButton.AddItem(Loc.GetString("humanoid-profile-editor-preference-backpack"), (int) BackpackPreference.Backpack);
            _backpackButton.AddItem(Loc.GetString("humanoid-profile-editor-preference-satchel"), (int) BackpackPreference.Satchel);
            _backpackButton.AddItem(Loc.GetString("humanoid-profile-editor-preference-duffelbag"), (int) BackpackPreference.Duffelbag);

            _backpackButton.OnItemSelected += args =>
            {
                _backpackButton.SelectId(args.Id);
                SetBackpack((BackpackPreference) args.Id);
            };

            #endregion Backpack

            #region Eyes

            _eyesPicker.OnEyeColorPicked += newColor =>
            {
                if (Profile is null)
                    return;
                Profile = Profile.WithCharacterAppearance(
                    Profile.Appearance.WithEyeColor(newColor));
                IsDirty = true;
            };

            #endregion Eyes

            #endregion Appearance

            #region Jobs

            _tabContainer.SetTabTitle(1, Loc.GetString("humanoid-profile-editor-jobs-tab"));

            _preferenceUnavailableButton.AddItem(
                Loc.GetString("humanoid-profile-editor-preference-unavailable-stay-in-lobby-button"),
                (int) PreferenceUnavailableMode.StayInLobby);
            _preferenceUnavailableButton.AddItem(
                Loc.GetString("humanoid-profile-editor-preference-unavailable-spawn-as-overflow-button",
                              ("overflowJob", Loc.GetString(SharedGameTicker.OverflowJobName))),
                (int) PreferenceUnavailableMode.SpawnAsOverflow);

            _preferenceUnavailableButton.OnItemSelected += args =>
            {
                _preferenceUnavailableButton.SelectId(args.Id);

                Profile = Profile?.WithPreferenceUnavailable((PreferenceUnavailableMode) args.Id);
                IsDirty = true;
            };

            _jobPriorities = new List<JobPrioritySelector>();
            _jobCategories = new Dictionary<string, BoxContainer>();

            var firstCategory = true;

            foreach (var job in prototypeManager.EnumeratePrototypes<JobPrototype>().OrderBy(j => j.Name))
            {
                foreach (var department in job.Departments)
                {
                    if (!_jobCategories.TryGetValue(department, out var category))
                    {
                        category = new BoxContainer
                        {
                            Orientation = LayoutOrientation.Vertical,
                            Name = department,
                            ToolTip = Loc.GetString("humanoid-profile-editor-jobs-amount-in-department-tooltip",
                                                    ("departmentName", department))
                        };

                            if (firstCategory)
                            {
                                firstCategory = false;
                            }
                            else
                            {
                                category.AddChild(new Control
                                {
                                    MinSize = new Vector2(0, 23),
                                });
                            }

                        category.AddChild(new PanelContainer
                        {
                            PanelOverride = new StyleBoxFlat {BackgroundColor = Color.FromHex("#464966")},
                            Children =
                            {
                                new Label
                                {
                                    Text = Loc.GetString("humanoid-profile-editor-department-jobs-label",
                                                         ("departmentName" ,department))
                                }
                            }
                        });

                        _jobCategories[department] = category;
                        _jobList.AddChild(category);
                    }

                    var selector = new JobPrioritySelector(job);
                    category.AddChild(selector);
                    _jobPriorities.Add(selector);

                    selector.PriorityChanged += priority =>
                    {
                        Profile = Profile?.WithJobPriority(job.ID, priority);
                        IsDirty = true;

                        foreach (var jobSelector in _jobPriorities)
                        {
                            // Sync other selectors with the same job in case of multiple department jobs
                            if (jobSelector.Job == selector.Job)
                            {
                                jobSelector.Priority = priority;
                            }

                            // Lower any other high priorities to medium.
                            if (priority == JobPriority.High)
                            {
                                if (jobSelector.Job != selector.Job && jobSelector.Priority == JobPriority.High)
                                {
                                    jobSelector.Priority = JobPriority.Medium;
                                    Profile = Profile?.WithJobPriority(jobSelector.Job.ID, JobPriority.Medium);
                                }
                            }
                        }
                    };
                }
            }

            #endregion Jobs

            #region Antags

            _tabContainer.SetTabTitle(2, Loc.GetString("humanoid-profile-editor-antags-tab"));

            _antagPreferences = new List<AntagPreferenceSelector>();

            foreach (var antag in prototypeManager.EnumeratePrototypes<AntagPrototype>().OrderBy(a => a.Name))
            {
                if (!antag.SetPreference)
                {
                    continue;
                }

                var selector = new AntagPreferenceSelector(antag);
                _antagList.AddChild(selector);
                _antagPreferences.Add(selector);

                selector.PreferenceChanged += preference =>
                {
                    Profile = Profile?.WithAntagPreference(antag.ID, preference);
                    IsDirty = true;
                };
            }

            #endregion Antags

            #region Save

            _saveButton.OnPressed += args => { Save(); };

            #endregion Save

            #endregion Left

            #region Right

            #region Preview

            _previewDummy = entityManager.SpawnEntity("MobHumanDummy", MapCoordinates.Nullspace);
            var sprite = _previewDummy.GetComponent<SpriteComponent>();

            // Front
            _previewSprite = new SpriteView
            {
                Sprite = sprite,
                Scale = (6, 6),
                OverrideDirection = Direction.South,
                VerticalAlignment = VAlignment.Center,
                SizeFlagsStretchRatio = 1
            };
            _previewSpriteControl.AddChild(_previewSprite);

            // Side
            _previewSpriteSide = new SpriteView
            {
                Sprite = sprite,
                Scale = (6, 6),
                OverrideDirection = Direction.East,
                VerticalAlignment = VAlignment.Center,
                SizeFlagsStretchRatio = 1
            };
            _previewSpriteSideControl.AddChild(_previewSpriteSide);

            #endregion Right

            #endregion

            if (preferencesManager.ServerDataLoaded)
            {
                LoadServerData();
            }

            preferencesManager.OnServerDataLoaded += LoadServerData;

            IsDirty = false;
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (!disposing)
                return;

            _previewDummy.Delete();
            _preferencesManager.OnServerDataLoaded -= LoadServerData;
        }

        private void LoadServerData()
        {
            Profile = (HumanoidCharacterProfile) _preferencesManager.Preferences!.SelectedCharacter;
            CharacterSlot = _preferencesManager.Preferences.SelectedCharacterIndex;
            UpdateControls();
        }

        private void SetAge(int newAge)
        {
            Profile = Profile?.WithAge(newAge);
            IsDirty = true;
        }

        private void SetSex(Sex newSex)
        {
            Profile = Profile?.WithSex(newSex);
            IsDirty = true;
        }

        private void SetGender(Gender newGender)
        {
            Profile = Profile?.WithGender(newGender);
            IsDirty = true;
        }

        private void SetName(string newName)
        {
            Profile = Profile?.WithName(newName);
            IsDirty = true;
        }

        private void SetClothing(ClothingPreference newClothing)
        {
            Profile = Profile?.WithClothingPreference(newClothing);
            IsDirty = true;
        }

        private void SetBackpack(BackpackPreference newBackpack)
        {
            Profile = Profile?.WithBackpackPreference(newBackpack);
            IsDirty = true;
        }

        public void Save()
        {
            IsDirty = false;

            if (Profile != null)
            {
                _preferencesManager.UpdateCharacter(Profile, CharacterSlot);
                OnProfileChanged?.Invoke(Profile, CharacterSlot);
            }
        }

        private bool IsDirty
        {
            get => _isDirty;
            set
            {
                _isDirty = value;
                _needUpdatePreview = true;
                UpdateSaveButton();
            }
        }


        private void UpdateNameEdit()
        {
            _nameEdit.Text = Profile?.Name ?? "";
        }

        private void UpdateAgeEdit()
        {
            _ageEdit.Text = Profile?.Age.ToString() ?? "";
        }

        private void UpdateSexControls()
        {
            if (Profile?.Sex == Sex.Male)
                _sexMaleButton.Pressed = true;
            else
                _sexFemaleButton.Pressed = true;
        }

        private void UpdateSkinColor()
        {
            if (Profile == null)
                return;

            var color = Color.ToHsv(Profile.Appearance.SkinColor);
            // check for hue/value first, if hue is lower than this percentage
            // and value is 1.0
            // then it'll be hue
            if (Math.Clamp(color.X, 25f / 360f, 1) > 25f / 360f
                && color.Z == 1.0)
            {
                _skinColor.Value = Math.Abs(45 - (color.X * 360));
            }
            // otherwise it'll directly be the saturation
            else
            {
                _skinColor.Value = color.Y * 100;
            }
        }

        private void UpdateGenderControls()
        {
            if (Profile == null)
            {
                return;
            }

            _genderButton.SelectId((int) Profile.Gender);
        }

        private void UpdateClothingControls()
        {
            if (Profile == null)
            {
                return;
            }

            _clothingButton.SelectId((int) Profile.Clothing);
        }

        private void UpdateBackpackControls()
        {
            if (Profile == null)
            {
                return;
            }

            _backpackButton.SelectId((int) Profile.Backpack);
        }

        private void UpdateHairPickers()
        {
            if (Profile == null)
            {
                return;
            }

            _hairPicker.SetData(
                Profile.Appearance.HairColor,
                Profile.Appearance.HairStyleId,
                SpriteAccessoryCategories.HumanHair,
                true);
            _facialHairPicker.SetData(
                Profile.Appearance.FacialHairColor,
                Profile.Appearance.FacialHairStyleId,
                SpriteAccessoryCategories.HumanFacialHair,
                true);
        }

        private void UpdateEyePickers()
        {
            if (Profile == null)
            {
                return;
            }

            _eyesPicker.SetData(Profile.Appearance.EyeColor);
        }

        private void UpdateSaveButton()
        {
            _saveButton.Disabled = Profile is null || !IsDirty;
        }

        private void UpdatePreview()
        {
            if (Profile is null)
                return;

            EntitySystem.Get<SharedHumanoidAppearanceSystem>().UpdateFromProfile(_previewDummy.Uid, Profile);
            LobbyCharacterPreviewPanel.GiveDummyJobClothes(_previewDummy, Profile);
        }

        public void UpdateControls()
        {
            if (Profile is null) return;
            UpdateNameEdit();
            UpdateSexControls();
            UpdateGenderControls();
            UpdateSkinColor();
            UpdateClothingControls();
            UpdateBackpackControls();
            UpdateAgeEdit();
            UpdateHairPickers();
            UpdateEyePickers();
            UpdateSaveButton();
            UpdateJobPriorities();
            UpdateAntagPreferences();

            _needUpdatePreview = true;

            _preferenceUnavailableButton.SelectId((int) Profile.PreferenceUnavailable);
        }

        protected override void FrameUpdate(FrameEventArgs args)
        {
            base.FrameUpdate(args);

            if (_needUpdatePreview)
            {
                UpdatePreview();

                _needUpdatePreview = false;
            }
        }

        private void UpdateJobPriorities()
        {
            foreach (var prioritySelector in _jobPriorities)
            {
                var jobId = prioritySelector.Job.ID;

                var priority = Profile?.JobPriorities.GetValueOrDefault(jobId, JobPriority.Never) ?? JobPriority.Never;

                prioritySelector.Priority = priority;
            }
        }

        private class JobPrioritySelector : Control
        {
            public JobPrototype Job { get; }
            private readonly RadioOptions<int> _optionButton;

            public JobPriority Priority
            {
                get => (JobPriority) _optionButton.SelectedValue;
                set => _optionButton.SelectByValue((int) value);
            }

            public event Action<JobPriority>? PriorityChanged;

            public JobPrioritySelector(JobPrototype job)
            {
                Job = job;

                _optionButton = new RadioOptions<int>(RadioOptionsLayout.Horizontal)
                {
                    FirstButtonStyle = StyleBase.ButtonOpenRight,
                    ButtonStyle = StyleBase.ButtonOpenBoth,
                    LastButtonStyle = StyleBase.ButtonOpenLeft
                };

                // Text, Value
                _optionButton.AddItem(Loc.GetString("humanoid-profile-editor-job-priority-high-button"), (int) JobPriority.High);
                _optionButton.AddItem(Loc.GetString("humanoid-profile-editor-job-priority-medium-button"), (int) JobPriority.Medium);
                _optionButton.AddItem(Loc.GetString("humanoid-profile-editor-job-priority-low-button"), (int) JobPriority.Low);
                _optionButton.AddItem(Loc.GetString("humanoid-profile-editor-job-priority-never-button"), (int) JobPriority.Never);

                _optionButton.OnItemSelected += args =>
                {
                    _optionButton.Select(args.Id);
                    PriorityChanged?.Invoke(Priority);
                };

                var icon = new TextureRect
                {
                    TextureScale = (2, 2),
                    Stretch = TextureRect.StretchMode.KeepCentered
                };

                if (job.Icon != null)
                {
                    var specifier = new SpriteSpecifier.Rsi(new ResourcePath("/Textures/Interface/Misc/job_icons.rsi"),
                        job.Icon);
                    icon.Texture = specifier.Frame0();
                }

                AddChild(new BoxContainer
                {
                    Orientation = LayoutOrientation.Horizontal,
                    Children =
                    {
                        icon,
                        new Label {Text = job.Name, MinSize = (175, 0)},
                        _optionButton
                    }
                });
            }
        }

        private void UpdateAntagPreferences()
        {
            foreach (var preferenceSelector in _antagPreferences)
            {
                var antagId = preferenceSelector.Antag.ID;
                var preference = Profile?.AntagPreferences.Contains(antagId) ?? false;

                preferenceSelector.Preference = preference;
            }
        }

        private class AntagPreferenceSelector : Control
        {
            public AntagPrototype Antag { get; }
            private readonly CheckBox _checkBox;

            public bool Preference
            {
                get => _checkBox.Pressed;
                set => _checkBox.Pressed = value;
            }

            public event Action<bool>? PreferenceChanged;

            public AntagPreferenceSelector(AntagPrototype antag)
            {
                Antag = antag;

                _checkBox = new CheckBox {Text = $"{antag.Name}"};
                _checkBox.OnToggled += OnCheckBoxToggled;

                AddChild(new BoxContainer
                {
                    Orientation = LayoutOrientation.Horizontal,
                    Children =
                    {
                        _checkBox
                    }
                });
            }

            private void OnCheckBoxToggled(BaseButton.ButtonToggledEventArgs args)
            {
                PreferenceChanged?.Invoke(Preference);
            }
        }
    }
}
