# language: pl
Potrzeba biznesowa: Operacje na sesji wsadowej

@smoke @Batch @regresja
Scenariusz: Wys�anie dokument�w w jednocz�ciowej paczce
  Zak�adaj�c, �e mam 5 faktur z poprawnym NIP
  Je�eli wy�l� je w jednocz�ciowej zaszyfrowanej paczce
  Wtedy wszystkie faktury powinny by� przetworzone pomy�lnie
  Oraz powinienem m�c pobra� UPO

@smoke @Batch @regresja @Negative
Scenariusz: Wys�anie jednocz�ciowej paczki dokument�w ze z�ym NIP
  Zak�adaj�c, �e mam 5 faktur z niepoprawnym NIP
  Je�eli wy�l� je w jednocz�ciowej zaszyfrowanej paczce
  Wtedy proces powinien zako�czy� si� b��dem 445
  Oraz wszystkie faktury powinny by� odrzucone

@Batch @regresja @Negative
Scenariusz: Przekroczenie limitu liczby faktur
  Zak�adaj�c, �e mam 10001 faktur
  Je�eli wy�l� je w paczce
  Wtedy system powinien zwr�ci� b��d 420

@Batch @regresja @Negative
Scenariusz: Przekroczenie maksymalnego rozmiaru ca�ej paczki
  Zak�adaj�c, �e przygotowa�em paczk� o deklarowanym rozmiarze wi�kszym ni� 5 GiB
  Je�eli spr�buj� nawi�za� sesj� wsadow�
  Wtedy system odrzuci ��danie ze wzgl�du na przekroczenie limitu fileSize

@Batch @regresja @Negative
Scenariusz: Przekroczenie rozmiaru paczki
  Zak�adaj�c, �e mam paczk� o rozmiarze 101 MiB
  Je�eli spr�buj� otworzy� sesj� wsadow�
  Wtedy system powinien odrzuci� ��danie

@Batch @regresja @Negative
Scenariusz: Zamkni�cie sesji bez wys�ania wszystkich cz�ci
  Zak�adaj�c, �e zadeklarowa�em 3 cz�ci paczki
  Je�eli wy�l� tylko 1 cz��
  Wtedy system powinien odrzuci� pr�b� wys�ania

@Batch @regresja @Negative  
Scenariusz: Przekroczenie limitu liczby cz�ci
  Zak�adaj�c, �e zadeklarowa�em 51 cz�ci paczki
  Je�eli spr�buj� otworzy� sesj� wsadow�
  Wtedy system powinien odrzuci� ��danie

@Batch @regresja @Encryption @Negative
Scenariusz: Nieprawid�owy zaszyfrowany klucz
  Zak�adaj�c, �e mam paczk� z uszkodzonym kluczem szyfrowania
  Je�eli wy�l� paczk� i zamkn� sesj�
  Wtedy system powinien zwr�ci� b��d 415

@Batch @regresja @Encryption @Negative
Scenariusz: Uszkodzone zaszyfrowane dane
  Zak�adaj�c, �e mam paczk� z uszkodzonymi danymi
  Je�eli wy�l� paczk� i zamkn� sesj�
  Wtedy system powinien zwr�ci� b��d 405

@Batch @regresja @Encryption @Negative
Scenariusz: Nieprawid�owy wektor inicjuj�cy
  Zak�adaj�c, �e mam paczk� z nieprawid�owym IV
  Je�eli wy�l� paczk� i zamkn� sesj�
  Wtedy system powinien zwr�ci� b��d 430
