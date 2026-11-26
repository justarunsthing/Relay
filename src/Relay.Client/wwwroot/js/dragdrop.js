(() => {
let dotNetRef = null;
let draggedId = null;
let draggedCard = null;
let highlighted = null;

window.relayDrag = {
    start(ref) {
        if (dotNetRef) {
            return;
        }

        dotNetRef = ref;
        document.addEventListener("dragstart", onDragStart);
        document.addEventListener("dragover", onDragOver);
        document.addEventListener("drop", onDrop);
        document.addEventListener("dragend", onDragEnd);
    },
    stop() {
        document.removeEventListener("dragstart", onDragStart);
        document.removeEventListener("dragover", onDragOver);
        document.removeEventListener("drop", onDrop);
        document.removeEventListener("dragend", onDragEnd);
        clearDrag();
        dotNetRef = null;
    }
};

function onDragStart(event) {
    const target = elementFrom(event.target);
    const card = target?.closest("[data-card-id]");
    if (!card || !card.closest("[data-board]")) {
        return;
    }

    if (target?.closest("button, input, textarea, a, form")) {
        event.preventDefault();
        return;
    }

    draggedId = card.dataset.cardId;
    draggedCard = card;
    card.classList.add("dragging");
    event.dataTransfer.effectAllowed = "move";
    event.dataTransfer.setData("text/plain", draggedId);
}

function onDragOver(event) {
    const column = columnFrom(event.target);
    if (!column || !draggedId) {
        return;
    }

    event.preventDefault();
    event.dataTransfer.dropEffect = "move";
    if (highlighted !== column) {
        highlighted?.classList.remove("drop-target");
        column.classList.add("drop-target");
        highlighted = column;
    }
}

async function onDrop(event) {
    const column = columnFrom(event.target);
    const cardId = draggedId;
    if (!column || !cardId || !dotNetRef) {
        clearDrag();
        return;
    }

    event.preventDefault();
    const columnId = column.dataset.columnId;
    const index = dropIndex(column, event.clientY, cardId);
    clearDrag();

    try {
        await dotNetRef.invokeMethodAsync("OnCardDropped", cardId, columnId, index);
    } catch {
        // The page reports a failed move itself.
    }
}

function onDragEnd() {
    clearDrag();
}

function dropIndex(column, clientY, cardId) {
    const cards = [...column.querySelectorAll("[data-card-id]")]
        .filter(card => card.dataset.cardId !== cardId);

    for (let index = 0; index < cards.length; index++) {
        const rect = cards[index].getBoundingClientRect();
        if (clientY < rect.top + rect.height / 2) {
            return index;
        }
    }

    return cards.length;
}

function columnFrom(target) {
    return elementFrom(target)?.closest("[data-column-id]") ?? null;
}

function elementFrom(target) {
    if (target instanceof Element) {
        return target;
    }

    return target?.parentElement ?? null;
}

function clearDrag() {
    draggedCard?.classList.remove("dragging");
    highlighted?.classList.remove("drop-target");
    draggedId = null;
    draggedCard = null;
    highlighted = null;
}
})();
