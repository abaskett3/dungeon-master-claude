#!/usr/bin/env bash
# Dice roller for DMC. Usage: bash scripts/roll.sh 1d20+5 2d6+3 2d20kh1+5 4d6kh3
#   NdS      roll N dice with S sides (N defaults to 1)
#   khK/klK  keep the highest/lowest K dice (advantage: 2d20kh1, disadvantage: 2d20kl1)
#   +M/-M    flat modifier

if [ $# -eq 0 ]; then
    echo "usage: roll.sh <dice> [<dice> ...]   e.g. 1d20+5 2d6+3 2d20kh1+5 4d6kh3"
    exit 1
fi

join() {
    local out="" x
    for x in "$@"; do out="${out:+$out, }$x"; done
    printf '%s' "$out"
}

failed=0
re='^([0-9]*)d([0-9]+)((kh|kl)([0-9]+))?([+-][0-9]+)?$'
for expr in "$@"; do
    lower=$(printf '%s' "$expr" | tr '[:upper:]' '[:lower:]')
    if ! [[ $lower =~ $re ]]; then
        echo "$expr: not a dice expression (try 1d20+5, 2d20kh1, 4d6kh3)"
        failed=1
        continue
    fi

    count=${BASH_REMATCH[1]:-1}
    sides=${BASH_REMATCH[2]}
    keep_mode=${BASH_REMATCH[4]}
    keep_n=${BASH_REMATCH[5]}
    mod=${BASH_REMATCH[6]:-0}
    count=$((10#$count)); sides=$((10#$sides)); mod=$((mod))

    if [ "$count" -lt 1 ] || [ "$count" -gt 100 ] || [ "$sides" -lt 2 ] || [ "$sides" -gt 1000 ]; then
        echo "$expr: out of range (1-100 dice, 2-1000 sides)"
        failed=1
        continue
    fi

    dice=()
    for ((i = 0; i < count; i++)); do
        dice+=($(( ((RANDOM << 15) | RANDOM) % sides + 1 )))
    done

    kept=("${dice[@]}")
    keep_text=""
    if [ -n "$keep_mode" ]; then
        keep_n=$((10#$keep_n))
        if [ "$keep_n" -lt 1 ] || [ "$keep_n" -gt "$count" ]; then
            echo "$expr: cannot keep $keep_n of $count dice"
            failed=1
            continue
        fi
        if [ "$keep_mode" = "kh" ]; then order="-rn"; else order="-n"; fi
        kept=($(printf '%s\n' "${dice[@]}" | sort $order | head -n "$keep_n"))
        keep_text=" keep [$(join "${kept[@]}")]"
    fi

    total=$mod
    for d in "${kept[@]}"; do total=$((total + d)); done

    mod_text=""
    if [ "$mod" -gt 0 ]; then mod_text=" +$mod"; fi
    if [ "$mod" -lt 0 ]; then mod_text=" $mod"; fi

    echo "$expr: [$(join "${dice[@]}")]$keep_text$mod_text = $total"
done

exit $failed
