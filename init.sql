SELECT 'CREATE DATABASE adventure'
WHERE NOT EXISTS (SELECT FROM pg_database WHERE datname = 'adventure')
\gexec
