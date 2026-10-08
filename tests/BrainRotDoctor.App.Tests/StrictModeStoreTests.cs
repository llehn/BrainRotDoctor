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
        { "id": "locked", "name": "Locked", "allowanceMinutes": 1, "allDay": true,
          "sites": [ { "label": "Locked", "url": "example.com/locked" } ] },
        { "id": "free", "name": "Free", "allowanceMinutes": 1, "allDay": true,
          "sites": [ { "label": "Free", "url": "example.com/free" } ] }
      ]
    }
    """;

    private static string TempFile(string name) =>
        Path.Combine(Path.GetTempPath(), "brainrotdoctor-tests", Guid.NewGuid().ToString("N"), name);

    [Fact]
    public void Locked_rules_are_restored_over_a_hand_edited_config()
    {
        var store = new StrictModeStore(TempFile("strict-mode.json"));
        StrictModeSnapshot snapshot = store.Activate(TimeSpan.FromHours(1), TwoRules, new[] { "locked" });

        Assert.True(snapshot.IsActive);
        Assert.True(snapshot.IsLocked("locked"));
        Assert.False(snapshot.IsLocked("free"));

        // The user deleted every rule by hand while strict mode ran.
        string restored = store.ApplyLock("""{ "rules": [] }""");
        BlockerConfiguration config = ConfigurationLoader.Load(restored);

        Assert.Equal(new[] { "locked" }, config.Rules.Select(r => r.Id));
        Assert.Contains(config.MatchingRules(new Uri("https://example.com/locked")), r => r.Id == "locked");
    }

    [Fact]
    public void Saving_may_add_rules_and_change_unlocked_ones_but_not_locked_ones()
    {
        var store = new StrictModeStore(TempFile("strict-mode.json"));
        store.Activate(TimeSpan.FromHours(1), TwoRules, new[] { "locked" });

        EditableConfiguration edited = EditableConfiguration.FromJson(TwoRules);
        edited.Rules[1].AllowanceMinutes = 30;
        edited.Rules.Add(new EditableConfiguration.EditableRule { Id = "new", Name = "New" });
        Assert.Null(store.FindLockViolations(edited));

        edited.Rules[0].AllowanceMinutes = 30;
        Assert.Equal(new[] { "Locked" }, store.FindLockViolations(edited));

        edited.Rules.RemoveAt(0);
        Assert.Equal(new[] { "Locked" }, store.FindLockViolations(edited));
    }

    [Fact]
    public void Locking_everything_does_not_lock_rules_added_later()
    {
        var store = new StrictModeStore(TempFile("strict-mode.json"));
        StrictModeSnapshot snapshot = store.Activate(TimeSpan.FromHours(1), TwoRules);

        Assert.True(snapshot.IsLocked("locked"));
        Assert.True(snapshot.IsLocked("free"));
        Assert.False(snapshot.IsLocked("added-later"));
    }

    [Fact]
    public void Nothing_is_locked_when_strict_mode_is_off()
    {
        var store = new StrictModeStore(TempFile("strict-mode.json"));
        Assert.False(store.GetSnapshot().IsActive);
        Assert.Equal(TwoRules, store.ApplyLock(TwoRules));
        Assert.Null(store.FindLockViolations(new EditableConfiguration()));
    }

    [Fact]
    public void A_pause_survives_a_restart_until_it_ends()
    {
        string path = TempFile("pause.json");
        DateTimeOffset now = DateTimeOffset.UtcNow;
        new PauseStore(path).Save(new PauseState(now.AddMinutes(15)));

        Assert.NotNull(new PauseStore(path).Load(now));
        Assert.Null(new PauseStore(path).Load(now.AddMinutes(16)));

        new PauseStore(path).Save(new PauseState(null));
        Assert.NotNull(new PauseStore(path).Load(now.AddYears(1)));

        new PauseStore(path).Save(null);
        Assert.Null(new PauseStore(path).Load(now));
    }
}
