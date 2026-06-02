#!/usr/bin/env python3
"""
Скрипт создания тестовых пользователей для PhotoHub.
Создаёт 1000 пользователей через API Gateway.
Использование: python3 seed_users.py
"""

import asyncio
import aiohttp
import random
import string
import time

API_URL = "http://localhost:5001/api/auth/register"  # напрямую в auth-service, минуя gateway и его rate limiter
TOTAL_USERS = 1000
BATCH_SIZE = 50  # параллельных запросов одновременно

def random_string(length=8):
    return ''.join(random.choices(string.ascii_lowercase + string.digits, k=length))

async def register_user(session, index):
    username = f"user_{index}_{random_string(4)}"
    payload = {
        "userName": username,
        "email": f"{username}@abobe.com",
        "password": "123456"
    }
    try:
        async with session.post(API_URL, json=payload, timeout=aiohttp.ClientTimeout(total=10)) as r:
            if r.status in (200, 201):
                return True, username
            else:
                text = await r.text()
                return False, f"[{r.status}] {text[:80]}"
    except Exception as e:
        return False, str(e)

async def main():
    print(f"Создаём {TOTAL_USERS} пользователей (батчами по {BATCH_SIZE})...")
    start = time.time()
    success = 0
    failed = 0

    async with aiohttp.ClientSession() as session:
        for batch_start in range(0, TOTAL_USERS, BATCH_SIZE):
            batch = range(batch_start, min(batch_start + BATCH_SIZE, TOTAL_USERS))
            tasks = [register_user(session, i) for i in batch]
            results = await asyncio.gather(*tasks)

            for ok, info in results:
                if ok:
                    success += 1
                else:
                    failed += 1
                    print(f"  ✗ Ошибка: {info}")

            print(f"  Прогресс: {batch_start + len(batch)}/{TOTAL_USERS} "
                  f"(✓ {success}  ✗ {failed})")

    elapsed = time.time() - start
    print(f"\nГотово за {elapsed:.1f}с")
    print(f"✓ Успешно: {success}")
    print(f"✗ Ошибок:  {failed}")

if __name__ == "__main__":
    asyncio.run(main())
