@validation @negative @regresja @qrcode
Szablon scenariusza: Budowanie linku weryfikacyjnego QR z certyfikatem tylko publicznym (bez klucza prywatnego)
Zakładając, że mam certyfikat X509 zawierający tylko część publiczną
Oraz obliczyłem hash faktury w formacie Base64
Jeżeli spróbuję zbudować link weryfikacyjny QR z użyciem tego certyfikatu
Wtedy zostanie zgłoszony błąd InvalidOperationException

Przykłady:
  | typKlucza |
  | RSA       |
  | ECC       |