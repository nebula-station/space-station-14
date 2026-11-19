// SPDX-FileCopyrightText: 2026 yellow-apple34
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Materials;
using Content.Shared.Paper;
using Content.Shared.Power.EntitySystems;
using Robust.Shared.GameObjects;

namespace Content.IntegrationTests.Tests._Nebula;

[TestFixture]
public sealed class DocumentPrinterPaperTest
{
    [Test]
    public async Task PrinterTakesOnlyBlankPaper()
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;
        var map = await pair.CreateTestMap();
        var entMan = server.EntMan;
        var storage = entMan.System<SharedMaterialStorageSystem>();
        var paperSys = entMan.System<PaperSystem>();
        var power = entMan.System<SharedPowerReceiverSystem>();

        EntityUid printer = default;
        EntityUid user = default;

        await server.WaitPost(() =>
        {
            printer = entMan.SpawnEntity("DocumentPrinter", map.GridCoords);
            user = entMan.SpawnEntity("MobHuman", map.GridCoords);
            power.SetNeedsPower(printer, false);
        });

        await pair.RunTicksSync(5);

        await server.WaitAssertion(() =>
        {
            var blank = entMan.SpawnEntity("Paper", map.GridCoords);
            Assert.That(storage.TryInsertMaterialEntity(user, blank, printer));
            Assert.That(storage.GetMaterialAmount(printer, "Paper"), Is.EqualTo(100));

            var written = entMan.SpawnEntity("Paper", map.GridCoords);
            paperSys.SetContent(written, "hello");
            Assert.That(storage.TryInsertMaterialEntity(user, written, printer), Is.False);

            var stamped = entMan.SpawnEntity("Paper", map.GridCoords);
            entMan.GetComponent<PaperComponent>(stamped).StampState = "paper_stamp-centcom";
            Assert.That(storage.TryInsertMaterialEntity(user, stamped, printer), Is.False);

            Assert.That(storage.GetMaterialAmount(printer, "Paper"), Is.EqualTo(100));
            Assert.That(entMan.EntityExists(written));
            Assert.That(entMan.EntityExists(stamped));
        });

        await pair.CleanReturnAsync();
    }
}
