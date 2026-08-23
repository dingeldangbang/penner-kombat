/** register.mjs — hängt den Resolve-Hook ein (node --import ./test/helpers/register.mjs). */
import { register } from 'node:module';
import { pathToFileURL } from 'node:url';
register('./loader.mjs', pathToFileURL(new URL('.', import.meta.url).pathname));
