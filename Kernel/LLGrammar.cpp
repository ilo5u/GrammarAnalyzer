#include "pch.h"
#include "LLGrammar.h"

/// <summary>
/// �Ƴ�����еĿհ��ַ��Լ��Ʊ��
/// </summary>
/// <param name="statement">����������</param>
/// <returns>��ʽ�����</returns>
static Statement RemoveSpace(const char statement[])
{
	Statement modifiedStatement{ };
	int length = (int)std::strlen(statement);
	std::for_each(statement, statement + length,
		[&modifiedStatement](char ch) {
		if (ch != ' ' && ch != '\t')
			modifiedStatement.push_back(ch);
	});
	return modifiedStatement;
}

/// <summary>
/// �����еķǷ��ַ��ϼ�
/// </summary>
/// <param name="ch"></param>
/// <returns></returns>
static bool InvalidCharacter(char ch)
{
	return (ch == ' ') || (ch == '\t') || (ch == '.') || (ch == '#') 
		|| (ch == '\0') || (ch == '\n') || (ch == '\r') || (ch == '|');
}

/// <summary>
/// ��������䰴���ض��ַ����зָ�Ϊ���ɸ��Ӿ�
/// </summary>
/// <param name="statement"></param>
/// <param name="separator"></param>
/// <returns></returns>
static Statements SplitStringBy(const Statement& statement, char separator)
{
	Statements result{ };
	Statement substatement{ };
	std::for_each(statement.begin(), statement.end(), 
		[&result, &substatement, separator](char ch) {
		if (ch == separator)
		{
			if (substatement.size() != 0)
			{
				result.push_back(substatement);
				substatement.clear();
			}
		}
		else
		{
			substatement.push_back(ch);
		}
	});
	if (substatement.size() > 0)
		result.push_back(substatement);
	return result;
}

static LLGrammar::First GetFirst(
	const LLGrammar::Candidate& candidate, 
	LLGrammar::FirstSet& firstSet
);

static LLGrammar::Follow GetFollow(
	const LLGrammar::Nonterminal& dest,
	const LLGrammar::ProductionSet& productions,
	LLGrammar::FirstSet& firstSet,
	LLGrammar::FollowSet& followSet,
	std::vector<LLGrammar::Nonterminal>& sours,
	std::map<LLGrammar::Nonterminal, bool>& doneList,
	const LLGrammar::Token& start
);

/// <summary>
/// ��ȡ���ż�
/// </summary>
/// <returns></returns>
LLGrammar::NoteSet LLGrammar::GetAllNotes() const
{
	return this->notes;
}

/// <summary>
/// ��ȡ����ʽ��������ѡʽ��
/// </summary>
/// <returns></returns>
LLGrammar::ProductionSet LLGrammar::GetAllProductions() const
{
	return this->productions;
}

/// <summary>
/// ��ȡ��ʼ���ս��
/// </summary>
/// <returns></returns>
LLGrammar::Nonterminal LLGrammar::GetStartNonterminal() const
{
	return this->start;
}

/// <summary>
/// ���ķ��в���һ���Ϸ����ս��
/// </summary>
/// <param name="statement">�ս�����ʽ</param>
/// <returns></returns>
bool LLGrammar::InsertTerminal(const char statement[])
{
	Statement modifiedStatement
		= RemoveSpace(statement);

	Terminal terminal{ Token::Type::TERMINAL };
	int pos = 0;
	while (!InvalidCharacter(modifiedStatement[pos]))
		terminal.attribute.push_back(modifiedStatement[pos++]);
	if (std::find(
		this->notes.begin(),
		this->notes.end(),
		terminal
	) == this->notes.end())
	{
		this->notes.insert(terminal);
		return true;
	}
	return false; // �����ظ�����
}

/// <summary>
/// ���ķ��в���һ���Ϸ��ķ��ս��
/// </summary>
/// <param name="statement">���ս�����ʽ</param>
/// <returns></returns>
bool LLGrammar::InsertNonterminal(const char statement[])
{
	Statement modifiedStatement
		= RemoveSpace(statement);

	Nonterminal nonterminal{ Token::Type::NONTERMINAL };
	int pos = 0;
	while (!InvalidCharacter(modifiedStatement[pos]))
		nonterminal.attribute.push_back(modifiedStatement[pos++]);
	if (std::find(
		this->notes.begin(),
		this->notes.end(),
		nonterminal
	) == this->notes.end())
	{
		this->notes.insert(nonterminal);
		return true;
	}
	return false; // �����ظ�����
}

bool LLGrammar::InsertProduction(const char statement[])
{
	Statement modifiedStatement
		= RemoveSpace(statement);

	Statements nonterminalAndCandidates
		= SplitStringBy(modifiedStatement, '#');
	if (nonterminalAndCandidates.size() != 2)
		return false; // �Ƿ����

	Nonterminal nonterminal{ Token::Type::NONTERMINAL };
	int pos = 0;
	while (!InvalidCharacter(nonterminalAndCandidates[0][pos]))
		nonterminal.attribute.push_back(nonterminalAndCandidates[0][pos++]);
	NoteSet::const_iterator note = std::find(
		this->notes.begin(),
		this->notes.end(),
		nonterminal
	);
	if (note == this->notes.end() 
		|| (note->type != Token::Type::NONTERMINAL))
	{
		return false; // �Ƿ�����ʽ
	}

	Statements candidates
		= SplitStringBy(nonterminalAndCandidates[1], '|');
	if (candidates.size() == 0)
		return false; // �޺Ϸ���ѡʽ

	/* ����Ϸ�����ʽ */
	volatile bool failed = false;
	Production production{ nonterminal };
	std::for_each(candidates.begin(), candidates.end(),
		[this, &production, &failed](const Statement& statement) {
		if (!failed)
		{
			Statements tokens
				= SplitStringBy(statement, '.');
			if (tokens.size() == 0)
				return; // �޺Ϸ�����

			/* ����Ϸ���ѡʽ */
			Candidate candidate;
			std::for_each(tokens.begin(), tokens.end(),
				[this, &candidate, &failed](const Statement& statement) {
				if (!failed)
				{
					int pos = 0;
					Token token{ Token::Type::TERMINAL };
					while (!InvalidCharacter(statement[pos]))
						token.attribute.push_back(statement[pos++]);

					NoteSet allNotes = this->GetAllNotes();
					NoteSet::const_iterator exist =
						std::find(allNotes.begin(), allNotes.end(), token);
					if (exist != allNotes.end())
					{
						token.type = exist->type;
						candidate.push_back(token);
					}
					else if (token.attribute.compare({ '@' }) == 0)
					{	// ��ѡʽΪEpsilon
						candidate.push_back(token);
					}
					else
					{
						failed = true; // �Ƿ����� �ò���ʽ����
					}
				}
			});

			/* ����Ϸ�����ʽ */
			if (!failed && candidate.size() != 0)
			{
				production.right = candidate;
				this->InsertProduction(production);
			}
		}
	});
	return !failed;
}

/// <summary>
/// ���ķ��в���һ���Ϸ��Ĳ���ʽ
/// </summary>
/// <param name="production"></param>
/// <returns></returns>
bool LLGrammar::InsertProduction(const Production& production)
{
	if (std::find(this->productions.begin(), this->productions.end(), production)
		== this->productions.end())
	{
		this->productions.push_back(production);
		return true;
	}
	return false; // �����ظ�����
}

/// <summary>
/// ������ʼ���ս��
/// </summary>
/// <param name="statement"></param>
/// <returns></returns>
bool LLGrammar::SetStartNonterminal(const char statement[])
{
	Statement modifiedStatement
		= RemoveSpace(statement);

	Nonterminal nonterminal{ Token::Type::NONTERMINAL };
	int pos = 0;
	while (!InvalidCharacter(modifiedStatement[pos]))
		nonterminal.attribute.push_back(modifiedStatement[pos++]);
	if (std::find(
		this->notes.begin(),
		this->notes.end(),
		nonterminal
	) == this->notes.end())
	{
		return false; // �Ƿ����ս��
	}
	this->start = nonterminal;
	return true;
}

/// <summary>
/// ����һ������ݹ顢�޹��������ӵ��ķ�
/// </summary>
/// <returns></returns>
LLGrammar LLGrammar::ToLL() const
{
	LLGrammar llGrammar = *this;
	llGrammar.isLL = true;
	llGrammar.EliminateLeftRecursion();
	llGrammar.EliminateCommonLeftFactor();
	return llGrammar;
}

#define DEBUG
std::string LLGrammar::GetAnalysisSheet()
{
	if (!isLL)
		*this = this->ToLL();

	/* ����FIRST���Լ�FOLLOW�� */
	FirstSet firstSet = WorkOutFirstSet();
	if (!firstSet.size())
		return false;

	FollowSet followSet = WorkOutFollowSet(firstSet);
	if (!followSet.size())
		return false;

	/* ������������������� */
	this->rows.clear();
	NonterminalSet nonterminals;
	int rowIndex = 0;
	for (const auto& token : this->notes)
	{
		if (token.type == Token::Type::NONTERMINAL)
		{
			nonterminals.insert(token);
			this->rows[token] = rowIndex++;
		}
	}

	TerminalSet terminals;
	terminals.insert(Token{ Token::Type::TERMINAL, '$' });
	std::for_each(this->notes.begin(), this->notes.end(),
		[&terminals](const Token& token) {
		if (token.type == Token::Type::TERMINAL)
			terminals.insert(token);
	});

	int columnIndex = 0;
	for (const auto& token : terminals)
		columns[token] = columnIndex++;
	for (const auto& token : nonterminals)
		columns[token] = columnIndex;

	/* ����LL(1)������ */
	for (const auto& production : this->productions)
	{
		First first = GetFirst(production.right, firstSet);
		for (const auto& elem : first)
		{
			if (elem.attribute.compare({ '@' }) == 0)
			{
				for (const auto& elem : followSet[production.left])
				{
					if (sheet.find({ this->rows[production.left], this->columns[elem] }) != sheet.end())
					{
						dpsheet[std::pair<int, int>{this->rows[production.left], this->columns[elem]}].push_back(production);
					}
					else
					{
						sheet[std::pair<int, int>{this->rows[production.left], this->columns[elem]}]
							= production;
					}
				}
			}
			else
			{
				if (sheet.find({ this->rows[production.left], this->columns[elem] }) != sheet.end())
				{
					dpsheet[std::pair<int, int>{this->rows[production.left], this->columns[elem]}].push_back(production);
				}
				else
				{
					sheet[std::pair<int, int>{this->rows[production.left], this->columns[elem]}]
						= production;
				}
			}
		}
	}

#ifdef DEBUG
	/* ���LL(1)������ */
	std::string description = { };
	description += " \t";
	for (const auto& terminal : terminals)
	{
		description += terminal.attribute;
		description += '\t';
	}
	description += '\n';

	for (const auto& nonterminal : nonterminals)
	{
		description += nonterminal.attribute;
		description += '\t';

		for (int column = 0; column < columnIndex; ++column)
		{
			if (sheet.find({ this->rows[nonterminal], column }) == sheet.end())
			{
				description += " \t";
			}
			else
			{
				description += sheet[std::pair<int, int>{this->rows[nonterminal], column}].left.attribute;
				description += "��";

				for (const auto& token : sheet[std::pair<int, int>{this->rows[nonterminal], column}].right)
				{
					if (token.attribute.compare({ '@' }) == 0)
						description += "��";
					else
						description += token.attribute;
				}

				if (dpsheet.find({ this->rows[nonterminal], column }) != dpsheet.end())
				{
					for (const auto& elem : dpsheet[std::pair<int, int>{this->rows[nonterminal], column}])
					{
						description += " �� ";
						description += elem.left.attribute;
						description += "��";

						for (const auto& token : elem.right)
						{
							if (token.attribute.compare({ '@' }) == 0)
								description += "��";
							else
								description += token.attribute;
						}
					}
				}
				description += '\t';
			}
		}
		description += '\n';
	}

#endif // DEBUG
	return description;
}

std::string LLGrammar::Analyze(const char _Word[])
{
	Statements words
		= SplitStringBy(_Word, ' ');
	/* ������������� */
	Tokens tokens;
	Token terminal{ Token::Type::TERMINAL };
	for (const auto& elem : words)
	{
		terminal.attribute = elem;
		tokens.push_back(terminal);
	}
	tokens.push_back({ Token::Type::TERMINAL, '$' });
	tokens.erase(std::find(tokens.begin(), tokens.end(), Token{ Token::Type::TERMINAL, '$' }) + 1,
		tokens.end());

	std::string description = { };
	description += "Step\tStack\tInput\tOutput\n";

	Tokens analysisStack{ {Token{ Token::Type::TERMINAL, '$' }, this->start} };
	Tokens::const_iterator input = tokens.begin();
	int step = 1;
	char buf[BUFSIZ] = { 0 };
	do
	{
		/* ���� */
		sprintf(buf, "%d", step);
		description += buf;
		description += '\t';
		step++;

		/* ջ */
		for (const auto& elem : analysisStack)
		{
			description += elem.attribute;
			description += ' ';
		}
		description += '\t';

		/* ���� */
		for (Tokens::const_iterator it = input; it != tokens.end(); ++it)
		{
			description += it->attribute;
			description += ' ';
		}
		description += '\t';

		Token top = analysisStack.back();
		if (top.type == Token::Type::TERMINAL)
		{
			if (top.attribute.compare(input->attribute) == 0)
			{
				analysisStack.pop_back();
				description += " \n";
				++input;
			}
			else
			{
				/* ��� */
				description += "�ǺŲ�����\n";
				return description;
			}
		}
		else
		{
			if (input->attribute.compare("$") != 0
				&& std::find(this->notes.begin(), this->notes.end(), *input)
				== this->notes.end())
			{
				/* ��� */
				description += "�ǺŲ�����\n";
				return description;
			}
			else
			{
				if (sheet.find({this->rows[top], this->columns[*input]}) != sheet.end())
				{
					/* ��� */
					description += sheet[std::pair<int, int>{this->rows[top], this->columns[*input]}].left.attribute + "��";
					for (const auto& elem : sheet[std::pair<int, int>{this->rows[top], this->columns[*input]}].right)
					{
						if (elem.attribute.compare({ '@' }) == 0)
							description += "��";
						else
							description += elem.attribute;
					}
					description += '\n';

					/* ���� */
					analysisStack.pop_back();
					for (Candidate::const_reverse_iterator it
						= sheet[std::pair<int, int>{this->rows[top], this->columns[*input]}].right.rbegin();
						it != sheet[std::pair<int, int>{this->rows[top], this->columns[*input]}].right.rend(); ++it)
					{
						if (it->attribute.compare({ '@' }) != 0)
						{
							analysisStack.push_back(*it);
						}
					}
				}
				else
				{
					NoteSet::const_iterator it = std::find_if(this->notes.begin(), this->notes.end(),
						[&tokens, &input](const Token& elem) {
						return elem == *input;
					});
					if (it != this->notes.end()
						&& it->type == Token::Type::NONTERMINAL
						&& top.attribute == it->attribute)
					{
						analysisStack.pop_back();
						++input;
						description += " \n";
					}
					else
					{
						/* ��� */
						description += "����������\n";
						return description;
					}
				}
			}
		}
	} while (analysisStack.back().attribute.compare("$") != 0 && input != tokens.end());
	/* ���� */
	sprintf(buf, "%d", step + 1);
	description += buf;
	description += '\t';
	step++;

	/* ջ */
	for (const auto& elem : analysisStack)
	{
		description += elem.attribute;
	}
	description += '\t';

	/* ���� */
	for (Tokens::const_iterator it = input; it != tokens.end(); ++it)
		description += it->attribute + " ";
	if (input == tokens.end())
		description += ' ';
	description += '\t';

	/* ��� */
	description += " \n";

	return description;
}

/// <summary>
/// ������ݹ�
/// </summary>
void LLGrammar::EliminateLeftRecursion()
{
	ProductionSet oldProductions
		= this->productions;
	this->productions.clear();

	NoteSet allNotes
		= this->notes;

	/* �ؽ��ķ� */
	std::for_each(this->notes.begin(), this->notes.end(), 
		[this, &oldProductions, &allNotes](const Token& nonterminal) {
		if (nonterminal.type == Token::Type::NONTERMINAL)
		{
			/* �ɼ���nonterminal���ɵĺ�ѡʽ */
			Candidates alphas;
			Candidates betas;
			std::for_each(oldProductions.begin(), oldProductions.end(),
				[&nonterminal, &alphas, &betas](Production& production) {
				if (production.left == nonterminal)
				{
					if (production.right[0] == nonterminal)
					{
						production.right.erase(production.right.begin());
						alphas.push_back(production.right);
					}
					else
					{
						betas.push_back(production.right);
					}
				}
			});

			if (alphas.size() > 0)
			{
				/* ������ݹ� */
				Nonterminal newNonterminal{ Token::Type::NONTERMINAL };
				newNonterminal.attribute = nonterminal.attribute;
				do
				{
					newNonterminal.attribute += '\'';
				} while (std::find(allNotes.begin(), allNotes.end(), newNonterminal) != allNotes.end());
				allNotes.insert(newNonterminal);

				this->InsertProduction({ newNonterminal, { Token{ } } });
				for (auto& candidate : alphas)
				{
					candidate.push_back(newNonterminal);
					this->InsertProduction({ newNonterminal, candidate });
				}
				for (auto& candidate : betas)
				{
					candidate.push_back(newNonterminal);
					this->InsertProduction({ nonterminal, candidate });
				}
			}
			else
			{
				for (const auto& candidate : betas)
				{
					this->InsertProduction({ nonterminal, candidate });
				}
			}
		}
	});

	this->notes = allNotes;
}

/// <summary>
/// ��������������
/// </summary>
void LLGrammar::EliminateCommonLeftFactor()
{
	bool noCommonFactor;
	do
	{
		noCommonFactor = true;
		ProductionSet oldProductions
			= this->productions;
		this->productions.clear();

		NoteSet allNotes
			= this->notes;

		/* �ؽ��ķ� */
		std::for_each(this->notes.begin(), this->notes.end(), 
			[this, &oldProductions, &allNotes, &noCommonFactor](const Token& nonterminal) {
			if (nonterminal.type == Token::Type::NONTERMINAL)
			{
				/* �ɼ�nonterminal���ɵĺ�ѡʽ */
				Candidates candidates;
				std::for_each(oldProductions.begin(), oldProductions.end(), [&nonterminal, &candidates](Production& production) {
					if (production.left == nonterminal)
					{
						candidates.push_back(production.right);
					}
				});
				std::vector<Candidates::const_iterator> merge; // ���ϲ���
				std::vector<Candidates::const_iterator> rest; // ������
				for (Candidates::const_iterator main = candidates.begin(); main != candidates.end(); ++main)
				{
					for (Candidates::const_iterator sub = candidates.begin(); sub != candidates.end(); ++sub)
					{
						if (main != sub && main->front() == sub->front())
							merge.push_back(sub);
						else if (main != sub)
							rest.push_back(sub);
					}
					if (merge.size() > 0)
					{ // �ҵ�ĳһ��Ŀ�ͷ���ڹ���������
						merge.push_back(main);
						break;
					}
					else
					{ // ������һ��
						rest.clear();
					}
				}
				if (merge.size() > 0)
				{
					/* ������ǰ���������ӵĺ�ѡʽ */
					std::for_each(rest.begin(), rest.end(), [this, &nonterminal](Candidates::const_iterator& candidate) {
						this->InsertProduction({ nonterminal, *candidate });
					});

					/* �ϲ����������� */
					Nonterminal newNonterminal{ Token::Type::NONTERMINAL };
					newNonterminal.attribute = nonterminal.attribute;
					do
					{
						newNonterminal.attribute += '\'';
					} while (std::find(allNotes.begin(), allNotes.end(), newNonterminal) != allNotes.end());
					allNotes.insert(newNonterminal);

					this->InsertProduction({ nonterminal, { merge[0]->front(), { newNonterminal } } });
					std::for_each(merge.begin(), merge.end(), [this, &newNonterminal](Candidates::const_iterator& candidate) {
						Candidate newCandidate = *candidate;
						newCandidate.erase(newCandidate.begin());
						if (newCandidate.size() == 0)
							/* ����ղ���ʽ */
							this->InsertProduction({ newNonterminal , { Token{ } } });
						else
							this->InsertProduction({ newNonterminal, newCandidate });
					});

					noCommonFactor = false;
				}
				else
				{
					/* �޹��������� */
					/* ������ǰ���������ӵĺ�ѡʽ */
					std::for_each(candidates.begin(), candidates.end(), [this, &nonterminal](const Candidate& candidate) {
						this->InsertProduction({ nonterminal, candidate });
					});
				}
			}
		});

		this->notes = allNotes;
	} while (!noCommonFactor);
}

void LLGrammar::Extend()
{
	Nonterminal newNonterminal{ Token::Type::NONTERMINAL };
	newNonterminal.attribute = this->start.attribute;
	do
	{
		newNonterminal.attribute += '\'';
	} while (std::find(this->notes.begin(), this->notes.end(), newNonterminal) != this->notes.end());
	this->notes.insert(newNonterminal);

	this->productions.push_back({ {newNonterminal}, {this->start} });
	this->start = newNonterminal;
}

/// <summary>
/// �ݹ�ɼ�ĳ�����ս����FIRST��
/// </summary>
/// <param name="candidate"></param>
/// <param name="firstSet"></param>
/// <returns></returns>
static LLGrammar::First GetFirst(const LLGrammar::Candidate& _Candidate, LLGrammar::FirstSet& _Firsts)
{
	LLGrammar::First first;
	LLGrammar::Token front = _Candidate.front();
	if (front.type == LLGrammar::Token::Type::TERMINAL)
	{
		first.insert(front);
	}
	else if (front.type == LLGrammar::Token::Type::NONTERMINAL)
	{
		if (_Firsts[front].size() > 0)
		{ // ��һ�����ս����FIRST����֪ ֱ�Ӻϲ�
			bool epsilon = false;
			std::for_each(_Firsts[front].begin(), _Firsts[front].end(),
				[&epsilon, &first](const LLGrammar::Terminal& elem) {
				if (elem.attribute.compare({'@'}) == 0)
					epsilon = true;
				else
					first.insert(elem); // �ϲ��ǿ�Ԫ��
			});
			if (epsilon)
			{
				/* �ɵ��� */
				LLGrammar::Candidate newCandidate = _Candidate;
				newCandidate.erase(newCandidate.begin());
				if (newCandidate.size() == 0)
				{
					first.insert(LLGrammar::Token{ }); // �޺���� ����epsilon
				}
				else
				{
					LLGrammar::First more = GetFirst(newCandidate, _Firsts);
					if (more.size() == 0)
					{
						return { }; // �ݹ����������FIRST���Ƿ���
					}
					else
					{ // FIRST���Ϸ�
						for (const auto& elem : more)
						{
							first.insert(elem);
						}
					}
				}
			}
		}
		else
		{
			// ��ʼ���ս����FIRST��δ֪ ��ǰ���ս��Ҳ�޷�������������޵ݹ飩
			// ���ؿռ��Ա����÷��ս����FIRST���Ƿ�
			return { };
		}
	}
	else if (front.attribute.compare({'@'}) == 0)
	{
		first.insert(LLGrammar::Terminal{});
	}
	return first;
}

/// <summary>
/// �ݹ�ɼ�ĳ�����ս����FOLLOW��
/// </summary>
/// <param name="dest">������ս��</param>
/// <param name="productions">����ʽ����</param>
/// <param name="firstSet"></param>
/// <param name="followSet"></param>
/// <param name="sours">�Ѿ����ʹ��ķ��ս��</param>
/// <param name="doneList">FOLLOW����֪�ķ��ս��</param>
/// <returns></returns>
static LLGrammar::Follow GetFollow(
	const LLGrammar::Nonterminal& _Dest,
	const LLGrammar::ProductionSet& _Productions,
	LLGrammar::FirstSet& _Firsts, 
	LLGrammar::FollowSet& _Follows, 
	std::vector<LLGrammar::Nonterminal>& _Sours,
	std::map<LLGrammar::Nonterminal, bool>& _DoneList,
	const LLGrammar::Token& _Start
)
{
	LLGrammar::Follow follow;
	LLGrammar::Candidate candidate;
	for (const auto& production : _Productions)
	{
		LLGrammar::Follow more;
		LLGrammar::Candidate::const_iterator next = production.right.begin();
		while ((next = std::find(next, production.right.end(), _Dest)) != production.right.end())
		{ // ������ս�������ڸò���ʽ��
			++next;
			bool epsilon = false;
			if (next != production.right.end())
			{
				LLGrammar::Candidate candidate{ next, production.right.end() };
				more = GetFirst(candidate, _Firsts);
				for (const auto& elem : more)
				{
					if (elem.attribute.compare({ '@' }) == 0)
						epsilon = true;
					else
						follow.insert(elem);
				}
			}
			if (epsilon
				|| next == production.right.end())
			{
				/* �ϲ�����ʽ�󲿵�FOLLOW�� */
				if (_DoneList[production.left])
				{ // �󲿵�FOLLOW����֪
					for (const auto& elem : _Follows[production.left])
					{
						follow.insert(elem);
					}
				}
				else if (production.left != _Sours[0]
					&& production.left != _Dest)
				{   // δ֪������󲿷��ս�����ǵ�һ��������ս�����뵱ǰ������ս����ͬʱ�Ž��еݹ�
					// �Ա������޵ݹ�
					if (std::find(_Sours.begin() + 1, _Sours.end(), production.left)
						== _Sours.end())
					{ // ���÷��շ�δ���ݹ���� ������ѷ��ʼ���
						_Sours.push_back(production.left);
						more = GetFollow(
							production.left,
							_Productions,
							_Firsts, _Follows,
							_Sours, _DoneList,
							_Start
						);
						if (_Sours.size() == 0)
							return { }; // ������ʧ��
						else
						{
							_Sours.pop_back();
							if (production.left == _Start)
								follow.insert(
									LLGrammar::Token{ LLGrammar::Token::Type::TERMINAL, {'$'} }
							);
							for (const auto& elem : more)
								follow.insert(elem);
						}
					}
					else
					{   // �������޵ݹ�
						// ���ѷ��ʼ�����Ϊ���Ա�־��ǰ������ʧ��
						_Sours.clear();
						return { };
					}
				}
			}
		}
	}
	return follow;
}

/// <summary>
/// ���FIRST��
/// </summary>
/// <returns></returns>
LLGrammar::FirstSet LLGrammar::WorkOutFirstSet() const
{
	FirstSet firstSet;
	ProductionSet oldProductions
		= this->productions;
	bool isDone;
	int count = 0;
	do
	{
		std::for_each(this->notes.begin(), this->notes.end(),
			[&firstSet, &isDone, &oldProductions](const Token& nonterminal) {
			if (nonterminal.type == LLGrammar::Token::Type::NONTERMINAL)
			{
				/* �ɼ�FIRST�� */
				First first;
				std::for_each(oldProductions.begin(), oldProductions.end(),
					[&firstSet, &nonterminal, &first](const Production& production) {
					if (production.left == nonterminal)
					{
						First more = GetFirst(production.right, firstSet);
						for (const auto& elem : more)
							first.insert(elem);
					}
				});

				for (const auto& elem : first)
				{
					if (firstSet[nonterminal].find(elem) == firstSet[nonterminal].end())
					{
						firstSet[nonterminal].insert(elem);
					}
				}
			}
		});
	} while (++count <= (int)this->notes.size() * (int)this->notes.size());
	return firstSet;
}

/// <summary>
/// ���FOLLOW��
/// </summary>
/// <param name="firstSet"></param>
/// <returns></returns>
LLGrammar::FollowSet LLGrammar::WorkOutFollowSet(FirstSet& _First) const
{
	FollowSet followSet;
	followSet[this->start].insert(Token{ Token::Type::TERMINAL, '$' });

	std::map<Token, bool> doneList;
	int count = 0;
	do
	{
		std::for_each(this->notes.begin(), this->notes.end(),
			[&doneList, this, &_First, &followSet](const Token& nonterminal) {
			if (nonterminal.type == Token::Type::NONTERMINAL
				&& doneList[nonterminal] == false)
			{
				/* �ɼ�FOLLOW�� */
				std::vector<Nonterminal> sours{ nonterminal };
				Follow follow
					= GetFollow(
						nonterminal,
						this->GetAllProductions(),
						_First, followSet,
						sours, doneList,
						this->GetStartNonterminal()
					);
				for (const auto& elem : follow)
				{
					followSet[nonterminal].insert(elem);
					doneList[nonterminal] = true;
				}
			}
		});

	} while (++count <= 2 * (int)this->notes.size());
	return followSet;
}
