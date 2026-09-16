"""Aceite HTTP/SQL local. Registro: 2026-09-14-02-cadastro-postgres-real.md."""
import json
import os
import secrets
import subprocess
import urllib.error
import urllib.request

BASE = 'http://127.0.0.1:5081'
tag = secrets.token_hex(12)
emails = [f'acceptance-{tag}-{kind}@example.test' for kind in ('courier', 'store')]
password = secrets.token_urlsafe(24)

def sql(query):
    return subprocess.check_output(
        ['docker', 'exec', '-i', 'coletas-preview-postgres-1', 'psql', '-X', '-v', 'ON_ERROR_STOP=1', '-U', 'coletas', '-d', os.environ['ACCEPTANCE_DB'], '-At'],
        input=query, text=True).strip()

def post(path, body, expected):
    request = urllib.request.Request(BASE + path, json.dumps(body).encode(), {'Content-Type': 'application/json'})
    try:
        with urllib.request.urlopen(request, timeout=15) as response:
            status, content = response.status, response.read()
    except urllib.error.HTTPError as error:
        status, content = error.code, error.read()
    assert status == expected, f'{path}: esperado {expected}, recebido {status}'
    print(f'HTTP {status}: {path}')
    return json.loads(content) if content else None

# Identificadores fictícios aleatórios evitam colisão entre execuções.
plate = ''.join(secrets.choice('ABCDEFGHIJKLMNOPQRSTUVWXYZ') for _ in range(3)) + '-' + str(secrets.randbelow(10000)).zfill(4)
cnpj = str(secrets.randbelow(10**12)).zfill(12)
for size in (12, 13):
    remainder = sum(int(cnpj[i]) * ((size - 1 - i) % 8 + 2) for i in range(size)) % 11
    cnpj += str(0 if remainder < 2 else 11 - remainder)
cpf = str(secrets.randbelow(10**9)).zfill(9)
for length in (9, 10):
    digit = sum(int(cpf[i]) * (length + 1 - i) for i in range(length)) * 10 % 11
    cpf += str(0 if digit == 10 else digit)
phone = '659' + str(secrets.randbelow(10**8)).zfill(8)
courier = dict(email=emails[0], password=password, fullName='Aceite local ficticio', phoneWhatsApp='+55' + phone, plate=plate, vehicleType='Motorcycle', cpf=cpf)
store = dict(email=emails[1], password=password, legalName='Aceite local ficticio LTDA', tradeName='Aceite local', taxId=cnpj, phoneWhatsApp=phone)
prefix = '/api/v1/auth/'
try:
    a = post(prefix + 'register/couriers', courier, 201)
    b = post(prefix + 'register/establishments', store, 201)
    # Results.Json usa o contrato numérico atual: UserStatus.Pending = 0.
    assert a['status'] == b['status'] == 0
    post(prefix + 'register/couriers', courier, 409)
    post(prefix + 'register/establishments', store, 409)
    post(prefix + 'register/couriers', {**courier, 'phoneWhatsApp': '123'}, 400)
    post(prefix + 'register/couriers', {**courier, 'plate': 'ABC-12345'}, 400)
    post(prefix + 'register/establishments', {**store, 'taxId': '00000000000000'}, 400)
    for email in emails:
        post(prefix + 'login', dict(email=email, password=password), 401)
    rows = sql(f'''SELECT c."PhoneWhatsApp", v."Plate", v."Type", u."Status" FROM identity."Users" u JOIN couriers."Couriers" c ON c."UserId"=u."Id" JOIN couriers."Vehicles" v ON v."CourierId"=c."Id" WHERE u."Email"='{emails[0]}';''')
    assert rows == f'{phone}|{plate.replace("-", "")}|Motorcycle|Pending', 'Persistência de entregador divergente'
    rows = sql(f'''SELECT e."PhoneWhatsApp", e."TaxId", u."Status" FROM identity."Users" u JOIN establishments."Establishments" e ON e."UserId"=u."Id" WHERE u."Email"='{emails[1]}';''')
    assert rows == f'{phone}|{cnpj}|Pending', 'Persistência de empresa divergente'
    assert sql(f'''SELECT count(*) FROM identity."Users" WHERE "Email" IN ('{emails[0]}','{emails[1]}');''') == '2'
    print('PASS: persistência SQL, normalização, vínculo do veículo, duplicidade, validações e bloqueio de login pendente.')
finally:
    # Exclusão restrita aos dois e-mails aleatórios gerados por esta execução; FKs removem dependentes.
    sql(f'''DELETE FROM identity."Users" WHERE "Email" IN ('{emails[0]}','{emails[1]}');''')
    assert sql(f'''SELECT count(*) FROM identity."Users" WHERE "Email" IN ('{emails[0]}','{emails[1]}');''') == '0'
    print('Dados fictícios desta execução removidos.')
