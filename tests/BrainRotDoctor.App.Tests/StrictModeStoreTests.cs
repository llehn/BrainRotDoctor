using BrainRotDoctor.App.Runtime;
using BrainRotDoctor.Core.Configuration;
using System.IO;
using Xunit;

namespace BrainRotDoctor.App.Tests;

public sealed class StrictModeStoreTests
{
    private const string TwoRules = """
    {
      "rules": [
        { "id": "on", "name": "On", "allowanceMinutes": 1, "allDay": true,
          "sites": [ { "label": "On", "url": "example.com/on" } ] },
        { "id": "off", "name": "Off", "enabled": false, "allowanceMinutes": 1, "allDay": true,
          "sites": [ { "label": "Off", "url": "example.com/off" } ] }
      ]
    }
    """;

    private static string TempFile(string name) =>
        Path.Combine(Path.GetTempPath(), "brainrotdoctor-tests", Guid.NewGuid().ToString("N"), name);

    [Fact]
    public void Strict_mode_is_a_flag_until_a_time_and_cannot_be_shortened()
    {
        var store = new StrictModeStore(TempFile("strict-mode.json"));
        Assert.False(store.GetSnapshot().IsActive);

        StrictModeSnapshot on = store.Activate(TimeSpan.FromHours(2));
        Assert.True(on.IsActive);
        Assert.InRange(on.Remaining, TimeSpan.FromMinutes(119), TimeSpan.FromHours(2));

        StrictModeSnapshot again = store.Activate(TimeSpan.FromMinutes(1));
        Assert.Equal(on.ActiveUntilLocal, again.ActiveUntilLocal);
    }

    [Fact]
    public void Adding_rules_and_switching_rules_on_only_tighten()
    {
        EditableConfiguration before = EditableConfiguration.FromJson(TwoRules);
        EditableConfiguration after = EditableConfiguration.FromJson(TwoRules);
        after.Rules[1].Enabled = true;
        after.Rules.Add(new EditableConfiguration.EditableRule { Id = "new", Name = "New" });

        Assert.Empty(RuleGuard.Loosened(before, after));
    }

    [Fact]
    public void Changing_switching_off_or_deleting_a_rule_loosens()
    {
        EditableConfiguration before = EditableConfiguration.FromJson(TwoRules);

        EditableConfiguration changed = EditableConfiguration.FromJson(TwoRules);
        changed.Rules[0].AllowanceMinutes = 30;
        Assert.Equal(new[] { "On" }, RuleGuard.Loosened(before, changed));

        EditableConfiguration switchedOff = EditableConfiguration.FromJson(TwoRules);
        switchedOff.Rules[0].Enabled = false;
        Assert.Equal(new[] { "On" }, RuleGuard.Loosened(before, switchedOff));

        EditableConfiguration deleted = EditableConfiguration.FromJson(TwoRules);
        deleted.Rules.RemoveAt(1);
        Assert.Equal(new[] { "Off" }, RuleGuard.Loosened(before, deleted));
    }

    [Fact]
    public void A_hand_edit_that_loosens_is_undone_but_one_that_tightens_stays()
    {
        // By hand: every rule deleted except "off", which was switched on, plus a new one.
        const string edited = """
        {
          "rules": [
            { "id": "off", "name": "Off", "allowanceMinutes": 1, "allDay": true,
              "sites": [ { "label": "Off", "url": "example.com/off" } ] },
            { "id": "new", "name": "New", "allowanceMinutes": 1, "allDay": true,
              "sites": [ { "label": "New", "url": "example.com/new" } ] }
          ]
        }
        """;

        // Only switched-on rules load, so "off" being there means it stayed on.
        BlockerConfiguration config = ConfigurationLoader.Load(RuleGuard.Restore(TwoRules, edited));

        Assert.Equal(new[] { "off", "new", "on" }, config.Rules.Select(r => r.Id));
    }

    [Fact]
    public void A_pause_survives_a_restart_until_it_ends()
    {
        string path = TempFile("pause.json");
        DateTimeOffset now = DateTimeOffset.UtcNow;
        new PauseStore(path).Save(new PauseState(now.AddMinutes(15)));

        Assert.NotNull(new PauseStore(path).Load(now));
        Assert.Null(new PauseStore(path).Load(now.AddMinutes(16)));

        new PauseStore(path).Save(new PauseState(now.AddYears(1)));
        Assert.Null(new PauseStore(path).Load(now));

        File.WriteAllText(path, "{\"UntilUtc\":null}");
        Assert.Null(new PauseStore(path).Load(now));

        new PauseStore(path).Save(null);
        Assert.Null(new PauseStore(path).Load(now));
    }
}
