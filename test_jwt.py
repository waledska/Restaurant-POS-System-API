import sys
import json
import urllib.request
import ssl

def main():
    ctx = ssl.create_default_context()
    ctx.check_hostname = False
    ctx.verify_mode = ssl.CERT_NONE

    req = urllib.request.Request('https://localhost:7286/api/Auth/login', data=json.dumps({'emailOrUserName':'admin', 'password':'admin123'}).encode('utf-8'), headers={'Content-Type': 'application/json'})
    try:
        with urllib.request.urlopen(req, context=ctx) as r:
            resp_body = r.read().decode('utf-8')
            resp = json.loads(resp_body)
    except Exception as e:
        print("Login failed:", e)
        return

    token = resp['data']['token']
    print(f"Token: {token[:20]}...")

    req2 = urllib.request.Request('https://localhost:7286/api/Auth/me', headers={'Authorization': f'Bearer {token}'})
    try:
        with urllib.request.urlopen(req2, context=ctx) as r:
            me_resp = r.read().decode('utf-8')
            print("Me Response: 200 OK")
            print(me_resp)
    except urllib.error.HTTPError as e:
        print("Me Error:", e.code)
        print(e.read().decode('utf-8'))

if __name__ == '__main__':
    main()
