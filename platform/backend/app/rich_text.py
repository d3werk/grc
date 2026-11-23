"""The stored editor schema deliberately has no HTML, styles, URLs or attachments."""

from typing import Annotated, Literal

from fastapi import HTTPException
from pydantic import BaseModel, ConfigDict, Field, model_validator


class EditorNode(BaseModel):
    model_config = ConfigDict(extra="forbid")


class Mark(EditorNode):
    type: Literal["bold", "italic"]


class Text(EditorNode):
    type: Literal["text"]
    text: Annotated[str, Field(min_length=1, max_length=30000, pattern=r"^[^\x00]*$")]
    marks: Annotated[list[Mark], Field(max_length=2)] | None = None


class HardBreak(EditorNode):
    type: Literal["hardBreak"]


Inline = Annotated[Text | HardBreak, Field(discriminator="type")]
Inlines = Annotated[list[Inline], Field(max_length=4096)]


class Paragraph(EditorNode):
    type: Literal["paragraph"]
    content: Inlines | None = None


class HeadingAttributes(EditorNode):
    level: Literal[2, 3]


class Heading(EditorNode):
    type: Literal["heading"]
    attrs: HeadingAttributes
    content: Inlines | None = None


class ListItem(EditorNode):
    type: Literal["listItem"]
    content: Annotated[list[Paragraph], Field(min_length=1, max_length=128)]


ListItems = Annotated[list[ListItem], Field(min_length=1, max_length=512)]


class BulletList(EditorNode):
    type: Literal["bulletList"]
    content: ListItems


class OrderedListAttributes(EditorNode):
    start: Annotated[int, Field(strict=True, ge=1, le=100000)] = 1
    type: None = None


class OrderedList(EditorNode):
    type: Literal["orderedList"]
    attrs: OrderedListAttributes | None = None
    content: ListItems


Block = Annotated[Paragraph | Heading | BulletList | OrderedList, Field(discriminator="type")]


def _plain_line(block: Paragraph | Heading) -> str:
    return "".join(node.text if isinstance(node, Text) else "\n" for node in block.content or [])


class RichTextDocument(EditorNode):
    type: Literal["doc"]
    content: Annotated[list[Block], Field(min_length=1, max_length=1024)]

    @model_validator(mode="after")
    def limit_document(self) -> "RichTextDocument":
        section_budget = 10000
        text_budget = 30000
        for block in self.content:
            lines = [block] if isinstance(block, (Paragraph, Heading)) else [
                line for item in block.content for line in item.content
            ]
            section_budget -= sum(1 + len(line.content or []) for line in lines)
            text_budget -= sum(len(node.text) for line in lines for node in line.content or [] if isinstance(node, Text))
            if section_budget < 0:
                raise ValueError("Der formatierte Text enthält zu viele Abschnitte.")
            if text_budget < 0:
                raise ValueError("Der formatierte Text darf höchstens 30000 Zeichen enthalten.")
        return self

    def plain_text(self) -> str:
        lines: list[str] = []
        for block in self.content:
            if isinstance(block, (Paragraph, Heading)):
                lines.append(_plain_line(block))
                continue
            start = block.attrs.start if isinstance(block, OrderedList) and block.attrs else 1
            for position, item in enumerate(block.content, start):
                prefix = f"{position}. " if isinstance(block, OrderedList) else "• "
                line = "\n".join(_plain_line(paragraph) for paragraph in item.content)
                if line.strip():
                    lines.append(prefix + line)
        return "\n".join(lines).strip()


RichTextFields = Annotated[dict[str, RichTextDocument], Field(max_length=16)]


def plain_projection(document: RichTextDocument, limit: int, *, required: bool = False) -> str:
    text = document.plain_text()
    if len(text) > limit:
        raise HTTPException(422, f"Der formatierte Text darf höchstens {limit} Zeichen enthalten.")
    if required and not text:
        raise HTTPException(422, "Geben Sie eine Begründung ein.")
    return text
