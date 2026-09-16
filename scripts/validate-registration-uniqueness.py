"""Corridas HTTP reais por identificador; registro 2026-09-14-04-identificadores-exclusivos-entregador.md."""
import concurrent.futures
import http.client
import json
import os
import secrets
import subprocess
import time
import urllib.error
import urllib.request

BASE = 'http://127.0.0.1:5081'
database = os.environ['ACCEPTANCE_DB']
assert database.startswith('registration_check_')

def sql(query):
    return subprocess.check_output(['docker', 'exec', '-i', 'coletas-preview-postgres-1', 'psql', '-X', '-v', 'ON_ERROR_STOP=1', '-U', 'coletas', '-d', database, '-At'], input=query, text=True).strip()

def document(cpf):
    digits = str(secrets.randbelow(10**(9 if cpf else 12))).zfill(9 if cpf else 12)
    for length in (range(9,11) if cpf else range(12,14)):
        total = sum(int(digits[i]) * (length + 1 - i if cpf else (length - 1 - i) % 8 + 2) for i in range(length))
        remainder = total * 10 % 11 if cpf else total % 11
        digits += str((0 if remainder == 10 else remainder) if cpf else (0 if remainder < 2 else 11 - remainder))
    return digits

def payload(courier):
    data = dict(email=f'uniqueness-{secrets.token_hex(12)}@example.test', password=secrets.token_urlsafe(24), phoneWhatsApp='659'+str(secrets.randbelow(10**8)).zfill(8))
    if courier:
        data.update(fullName='Teste isolado', cpf=document(True), plate=''.join(secrets.choice('ABCDEFGHIJKLMNOPQRSTUVWXYZ') for _ in range(3))+str(secrets.randbelow(10000)).zfill(4), vehicleType='Motorcycle')
    else:
        data.update(legalName='Teste isolado LTDA', tradeName='Teste isolado', taxId=document(False))
    return data

def post(kind, data):
    request = urllib.request.Request(BASE+'/api/v1/auth/register/'+kind, json.dumps(data).encode(), {'Content-Type':'application/json'})
    try:
        with urllib.request.urlopen(request, timeout=20) as response:
            return response.status
    except urllib.error.HTTPError as error:
        return error.code

for courier, fields in [(False, ['email','phoneWhatsApp','taxId']), (True, ['email','phoneWhatsApp','cpf','plate'])]:
    kind = 'couriers' if courier else 'establishments'
    for field in fields:
        # Reiniciar apenas o container temporário restaura a janela de rate limit sem alterar política.
        subprocess.run(['docker','restart',os.environ['ACCEPTANCE_CONTAINER']], check=True, stdout=subprocess.DEVNULL)
        for attempt in range(20):
            try:
                with urllib.request.urlopen(BASE+'/health/ready', timeout=2) as response:
                    if response.status == 200: break
            except (OSError, http.client.HTTPException): time.sleep(0.3)
        a, b = payload(courier), payload(courier)
        b[field] = a[field]
        if field == 'email': b[field] = ' '+a[field].upper()+' '
        if field == 'phoneWhatsApp': b[field] = '+55 ('+a[field][:2]+') '+a[field][2:7]+'-'+a[field][7:]
        if field == 'cpf': b[field] = a[field][:3]+'.'+a[field][3:6]+'.'+a[field][6:9]+'-'+a[field][9:]
        if field == 'plate': b[field] = a[field][:3].lower()+'-'+a[field][3:]
        addresses = sorted({a['email'].strip().lower(),b['email'].strip().lower()})
        clause = ','.join("'"+email+"'" for email in addresses)
        try:
            with concurrent.futures.ThreadPoolExecutor(max_workers=2) as pool:
                results = list(pool.map(lambda data: post(kind,data), [a,b]))
            assert sorted(results) == [201,409], f'{kind}/{field}: {results}'
            assert sql(f'''SELECT count(*) FROM identity."Users" WHERE "Email" IN ({clause});''') == '1'
            print(f'PASS concorrência {kind}/{field}: 201 + 409, um usuário persistido')
        finally:
            sql(f'''DELETE FROM identity."Users" WHERE "Email" IN ({clause});''')
print('PASS: sete cenários concorrentes em PostgreSQL isolado; dados removidos.')
