import os

import psycopg
import pytest

DATABASE_URL = os.environ.get("DATABASE_URL", "postgresql://quantlab:change_me@localhost:5432/quantlab")


@pytest.fixture
def db_conn():
    conn = psycopg.connect(DATABASE_URL)
    yield conn
    conn.rollback()
    conn.close()


@pytest.fixture
def asset_id(db_conn):
    with db_conn.cursor() as cur:
        cur.execute(
            """INSERT INTO assets (symbol, asset_name, exchange, currency)
               VALUES (%s, %s, %s, %s)
               ON CONFLICT (symbol) DO NOTHING
               RETURNING id""",
            ("TEST", "Test Asset", "TEST", "USD"),
        )
        row = cur.fetchone()
        if row is None:
            cur.execute("SELECT id FROM assets WHERE symbol = %s", ("TEST",))
            row = cur.fetchone()
    db_conn.commit()
    return row[0]
