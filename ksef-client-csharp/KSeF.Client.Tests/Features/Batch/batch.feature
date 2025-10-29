# language: pl
Potrzeba biznesowa: Operacje na sesji wsadowej

@smoke @Batch @regresja
Scenariusz: Wys³anie dokumentów w jednoczêœciowej paczce
  Zak³adaj¹c, ¿e mam 5 faktur z poprawnym NIP
  Je¿eli wyœlê je w jednoczêœciowej zaszyfrowanej paczce
  Wtedy wszystkie faktury powinny byæ przetworzone pomyœlnie
  Oraz powinienem móc pobraæ UPO

@smoke @Batch @regresja @Negative
Scenariusz: Wys³anie jednoczêœciowej paczki dokumentów ze z³ym NIP
  Zak³adaj¹c, ¿e mam 5 faktur z niepoprawnym NIP
  Je¿eli wyœlê je w jednoczêœciowej zaszyfrowanej paczce
  Wtedy proces powinien zakoñczyæ siê b³êdem 445
  Oraz wszystkie faktury powinny byæ odrzucone

@Batch @regresja @Negative
Scenariusz: Przekroczenie limitu liczby faktur
  Zak³adaj¹c, ¿e mam 10001 faktur
  Je¿eli wyœlê je w paczce
  Wtedy system powinien zwróciæ b³¹d 420

@Batch @regresja @Negative
Scenariusz: Przekroczenie maksymalnego rozmiaru ca³ej paczki
  Zak³adaj¹c, ¿e przygotowa³em paczkê o deklarowanym rozmiarze wiêkszym ni¿ 5 GiB
  Je¿eli spróbujê nawi¹zaæ sesjê wsadow¹
  Wtedy system odrzuci ¿¹danie ze wzglêdu na przekroczenie limitu fileSize

@Batch @regresja @Negative
Scenariusz: Przekroczenie rozmiaru paczki
  Zak³adaj¹c, ¿e mam paczkê o rozmiarze 101 MiB
  Je¿eli spróbujê otworzyæ sesjê wsadow¹
  Wtedy system powinien odrzuciæ ¿¹danie

@Batch @regresja @Negative
Scenariusz: Zamkniêcie sesji bez wys³ania wszystkich czêœci
  Zak³adaj¹c, ¿e zadeklarowa³em 3 czêœci paczki
  Je¿eli wyœlê tylko 1 czêœæ
  Wtedy system powinien odrzuciæ próbê wys³ania

@Batch @regresja @Negative  
Scenariusz: Przekroczenie limitu liczby czêœci
  Zak³adaj¹c, ¿e zadeklarowa³em 51 czêœci paczki
  Je¿eli spróbujê otworzyæ sesjê wsadow¹
  Wtedy system powinien odrzuciæ ¿¹danie

@Batch @regresja @Encryption @Negative
Scenariusz: Nieprawid³owy zaszyfrowany klucz
  Zak³adaj¹c, ¿e mam paczkê z uszkodzonym kluczem szyfrowania
  Je¿eli wyœlê paczkê i zamknê sesjê
  Wtedy system powinien zwróciæ b³¹d 415

@Batch @regresja @Encryption @Negative
Scenariusz: Uszkodzone zaszyfrowane dane
  Zak³adaj¹c, ¿e mam paczkê z uszkodzonymi danymi
  Je¿eli wyœlê paczkê i zamknê sesjê
  Wtedy system powinien zwróciæ b³¹d 405

@Batch @regresja @Encryption @Negative
Scenariusz: Nieprawid³owy wektor inicjuj¹cy
  Zak³adaj¹c, ¿e mam paczkê z nieprawid³owym IV
  Je¿eli wyœlê paczkê i zamknê sesjê
  Wtedy system powinien zwróciæ b³¹d 430
