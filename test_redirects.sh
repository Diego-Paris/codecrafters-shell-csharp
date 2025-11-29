#!/bin/bash
# Manual test script for redirection features

echo "=== Testing Stdout Redirect (>) ==="
echo "echo 'Hello World' > test_output/hello.txt"
echo ""

echo "=== Testing Stdout Append (>>) ==="
echo "echo 'Hello Emily' >> test_output/greetings.txt"
echo "echo 'Hello Maria' >> test_output/greetings.txt"
echo "cat test_output/greetings.txt"
echo ""

echo "=== Testing Stderr Redirect (2>) ==="
echo "cat nonexistent 2> test_output/error.txt"
echo "cat test_output/error.txt"
echo ""

echo "=== Testing Stderr Append (2>>) ==="
echo "cat error1 2>> test_output/all_errors.txt"
echo "cat error2 2>> test_output/all_errors.txt"
echo "cat test_output/all_errors.txt"
echo ""

echo "=== Testing Mixed (> and 2>) ==="
echo "cat test_output/hello.txt nonexistent > test_output/out.txt 2> test_output/err.txt"
echo "cat test_output/out.txt"
echo "cat test_output/err.txt"
echo ""

echo "=== Testing Overwrite then Append ==="
echo "echo 'List:' > test_output/list.txt"
echo "echo 'apple' >> test_output/list.txt"
echo "echo 'banana' >> test_output/list.txt"
echo "cat test_output/list.txt"
