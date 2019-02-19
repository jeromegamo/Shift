module Program = 
    open Shift.IntegrationTests.RemoveMigrationTests
    let [<EntryPoint>] main _ = 
        ``Should not remove the latest migration file if already applied to database`` ()
        // ``Should remove the latest migration file if not yet applied to database`` ()
        0
