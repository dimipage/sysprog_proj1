# sysprog_proj1

##Tekst zadatka:

###Zadatak 24:
Kreirati Web server koji klijentu omogućava pretragu knjiga korišćenjem Open Library API-a. Pretraga se može vršiti pomoću filtera koji se zadaju u okviru query-a. Spisak knjiga koje zadovoljavaju uslov se vraćaju kao odgovor klijentu. Svi zahtevi serveru se šalju preko browsera korišćenjem GET metode. Ukoliko navedene knjige ne postoje, prikazati grešku korisniku. 
* Način funkcionisanja Open Library API-a je moguće proučiti na sledećem linku: https://openlibrary.org/dev/docs/api/search
* Primer poziva serveru: https://openlibrary.org/search.json?author=tolkien&sort=new

##Zahtevi:

Implementirati serversku aplikaciju kao konzolni program u programskom jeziku C# koja obrađuje zahteve klijenata i generiše odgovore na osnovu pristupa deljenim resursima. Sistem mora podržati istovremeni rad sa većim brojem zahteva, pri čemu se prijem i obrada zahteva izvršavaju konkurentno. Arhitektura sistema treba da bude zasnovana na razdvajanju prijema i obrade zahteva. Pristigli zahtevi se smeštaju u deljenu strukturu podataka, dok njihovu obradu vrši skup niti korišćenjem ThreadPool-a ili sopstvene implementacije. Broj paralelnih obrada mora biti kontrolisan. Sinhronizacija između niti mora biti blokirajuća i realizovana korišćenjem mehanizama iz prostora
imena System.Threading, uključujući lock/Monitor i Monitor.Wait/Monitor.Pulse. Sistem treba da koristi zajedničke resurse (npr. memoriju, fajlove ili druge izvore podataka) koj su dostupni svim nitima, pri čemu je neophodno obezbediti njihov thread-safe pristup. Poseban akcenat staviti na pravilno upravljanje kritičnim sekcijama i izbegavanje problema konkurentnog pristupa. U okviru sistema potrebno je implementirati mehanizam keširanja koji omogućava ponovno korišćenje prethodno pribavljenih rezultata. Keš mora biti thread-safe i podržavati definisanu strategiju upravljanja (vremensko isticanje ili ograničenje veličine). U slučaju istovremenih
zahteva za istim resursom, obrada treba da se izvrši samo jednom, dok ostale niti čekaju na rezultat
(problem poznat kao cache stampede). Koristiti strategiju upravljanja koja je data uz konkretnu temu.vre Sistem mora da vodi evidenciju o obradi zahteva i stanju sistema. Logovanje mora biti realizovano na thread-safe način. Takođe je potrebno obezbediti odgovarajuću obradu grešaka i stabilno ponašanje sistema u uslovima većeg opterećenja. U okviru projekta potrebno je analizirati korišćene mehanizme sinhronizacije, identifikovati kritične sekcije i razmotriti ponašanje sistema pri različitim nivoima konkurentnog opterećenja (naročito u situacijama sa velikim brojem paralelnih zahteva).
