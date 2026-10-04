using System.Reflection;
using BomBom.Config;
using BomBom.Stealthsey;
using BomBom.Stealthsey.Reflection;

namespace BomBom.HideseyTests;

[TestFixture]
public class HideLevelTests
{
    [Test]
    public void HideLevelRequirement_Executes_AtRequiredLevel()
    {
        // Arrange
        BomBomConf.BomBomHide = HideLevel.Normal; // Set the current HideLevel to Normal
        MethodInfo method = typeof(TestClass).GetMethod("MethodWithRequirement")!;

        // Act
        bool canExecute = HideseyPatches.LevelCheck(method);

        // Assert
        Assert.That(canExecute, Is.True, "Method should execute at required HideLevel.");
    }

    [Test]
    public void HideLevelRequirement_Blocked_BelowRequiredLevel()
    {
        // Arrange
        BomBomConf.BomBomHide = HideLevel.Disabled; // Set the current HideLevel below the requirement
        MethodInfo method = typeof(TestClass).GetMethod("MethodWithRequirement")!;

        // Act
        bool canExecute = HideseyPatches.LevelCheck(method);

        // Assert
        Assert.That(canExecute, Is.False, "Method should be blocked below required HideLevel.");
    }

    [Test]
    public void HideLevelRestriction_Executes_BelowMaxLevel()
    {
        // Arrange
        BomBomConf.BomBomHide = HideLevel.Normal; // Set the current HideLevel below the maximum
        MethodInfo method = typeof(TestClass).GetMethod("MethodWithRestriction")!;

        // Act
        bool canExecute = HideseyPatches.LevelCheck(method);

        // Assert
        Assert.That(canExecute, Is.True, "Method should execute below maximum HideLevel.");
    }

    [Test]
    public void HideLevelRestriction_Blocked_AtOrAboveMaxLevel()
    {
        // Arrange
        BomBomConf.BomBomHide = HideLevel.Unconditional; // Set the current HideLevel at maximum
        MethodInfo method = typeof(TestClass).GetMethod("MethodWithRestriction")!;

        // Act
        bool canExecute = HideseyPatches.LevelCheck(method);

        // Assert
        Assert.That(canExecute, Is.False, "Method should be blocked at or above maximum HideLevel.");
    }
}

public class TestClass
{
    [HideLevelRequirement(HideLevel.Normal)]
    public void MethodWithRequirement()
    {
        Console.WriteLine("Test");
    }

    [HideLevelRestriction(HideLevel.Explicit)]
    public void MethodWithRestriction()
    {
        Console.WriteLine("Test");
    }
}
