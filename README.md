# 3c-update-pairs
Updates a 3commas composite bot's pairs based on ~~lunarcrush's altrank and galaxy scores~~ cryptobubbles 5 minute performance

Create a new c# console program and add XCommas.Net nuget package and paste the two .cs files into project directory.

Update 3commas key, secret, market, botId, and ~~MAX_ALTRANK_PAIRS, MAX_GALAXY_PAIRS~~ MAX_BUBBLE_PAIRS variables.

By default it will try and add the top 50 ~~altrank and the top 50 galaxy~~ cryptobubbles pairs if they exist on your exchange.
