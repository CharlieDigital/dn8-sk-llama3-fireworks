import './style.css'
import { fetchEventSource } from '@microsoft/fetch-event-source';

/**
 * Set up some helpers
 */
const $ = <T>(selector: string) => document.querySelector(selector) as T;
const $el = (selector: string) => $<HTMLElement>(selector);
const $btn = (selector: string) => $<HTMLButtonElement>(selector);
const $sel = (selector: string) => $<HTMLSelectElement>(selector);
const $txt = (selector: string) => $<HTMLTextAreaElement>(selector);

const $changed = (selector: string, handler: (evt: Event) => void) =>
  document.querySelector(selector)
  ?.addEventListener("input", handler);

const $clicked = (selector: string, handler: (evt: Event) => void) =>
  document.querySelector(selector)
  ?.addEventListener("click", handler);

/**
 * Reset method to clear the generated text.
 */
const reset = () => {
  $el("#add").innerHTML = "";
  $el("#ing").innerHTML = "";
  $el("#int").innerHTML = "";
  $el("#alt").innerHTML = "";
  $el("#ste").innerHTML = "";
  $el("#sde").innerHTML = "";
}

/**
 * Handle events when the textarea value changes
 */
$changed("textarea", (evt: Event) => {
  const value = (evt.target as HTMLTextAreaElement).value;

  if (value.trim().length < 12) {
    $btn("#generate")?.setAttribute("disabled", "true");
    return;
  }

  $btn("#generate")?.removeAttribute("disabled");
});

/**
 * Main action; click on the Generate button
 */
$clicked("#generate", async () => {
  reset();

  const prepTime = $sel("#time").value;
  const ingredientsOnHand = $txt("textarea").value;

  const controller = new AbortController();

  await fetchEventSource("http://localhost:5174/generate", {
    method: "POST",
    headers: {
      'Content-Type': 'application/json',
    },
    signal: controller.signal,
    openWhenHidden: true,
    body: JSON.stringify({
      ingredientsOnHand,
      prepTime
    }),
    onmessage: (msg) => {
      var payload = msg.data

      console.log(payload);

      var [part, content] = payload.split('|')

      if (!part || !$el(`#${part}`)) {
        return // Discard this message
      }

      content = content.replace(/⮑/gi, "\n")

      $el(`#${part}`).innerHTML += content
    },
    onclose: () => {
      console.log("Closed stream");
    },
    onerror: (err) => {
      console.error(err);
      controller.abort("Error");
    }
  })
});

$clicked("#reset", () => reset());
